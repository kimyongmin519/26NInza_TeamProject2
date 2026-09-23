using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    public class MoonReflectionAttack : MoonSkill
    {
        [Header("Repeat")]
        [SerializeField] private int phaseOneShotCount = 3;
        [SerializeField] private int phaseTwoShotCount = 2;
        [SerializeField] private float intervalBetweenShots = 0.25f;

        [Header("Movement")]
        [SerializeField] private float moveDuration = 0.55f;
        [SerializeField] private float returnDuration = 0.55f;
        [SerializeField, Range(0f, 1f)] private float horizontalRangeRate = 0.78f;
        [SerializeField] private Vector2 phaseOneHeightRate = new Vector2(0.35f, 0.78f);
        [SerializeField] private Vector2 phaseTwoHeightRate = new Vector2(-0.1f, 0.45f);
        [SerializeField] private float minimumPositionDistance = 4f;

        [Header("Beam")]
        [SerializeField] private float chargeDuration = 0.9f;
        [SerializeField] private float activeDuration = 0.28f;
        [SerializeField] private float beamWidth = 0.35f;
        [SerializeField] private float beamRangeMultiplier = 6f;
        [SerializeField] private int phaseOneBeamCount = 4;
        [SerializeField] private int phaseTwoBeamCount = 3;
        [SerializeField] private float phaseOneBeamSpread = 95f;
        [SerializeField] private float phaseTwoBeamSpread = 65f;
        [SerializeField] private float phaseOneDamage = 42f;
        [SerializeField] private float phaseTwoDamage = 58f;
        [SerializeField] private Color warningColor = Color.white;
        [SerializeField] private Color beamColor = new Color(1f, 0.82f, 0.3f, 1f);
        [SerializeField] private float flashWidthMultiplier = 1.8f;
        [SerializeField] private float flashDuration = 0.06f;

        [Header("Reflection")]
        [SerializeField] private LayerMask reflectionLayer = 1 << 3;
        [SerializeField, Min(0.001f)] private float reflectionSkin = 0.03f;

        private readonly List<TransformState> originStates = new List<TransformState>();
        private readonly List<MoonTelegraphLine> warningLines = new List<MoonTelegraphLine>();
        private readonly List<LineRenderer> beamLines = new List<LineRenderer>();
        private readonly List<DamageCaster> beamCasters = new List<DamageCaster>();
        private readonly List<BeamShot> currentShots = new List<BeamShot>();

        private Vector3 lastMainPosition;
        private bool hasLastMainPosition;

        public override bool CanUseSkill(GameObject target = null)
        {
            return Boss != null && Boss.Target != null && !Boss.IsDead;
        }

        protected override void OnMoonInitialize()
        {
            HideWarnings();
            HideBeams();
        }

        protected override IEnumerator ExecuteMoon(GameObject target)
        {
            CacheOrigins();
            hasLastMainPosition = false;

            int repeatCount = Boss.IsPhaseTwo
                ? Mathf.Max(1, phaseTwoShotCount)
                : Mathf.Max(1, phaseOneShotCount);

            for (int i = 0; i < repeatCount; i++)
            {
                MoveToNextPositions();
                yield return new WaitForSeconds(moveDuration / DurationScale);

                BuildShots();
                ShowWarnings();
                Boss.AttackReady(Boss.transform.position);
                yield return new WaitForSeconds(chargeDuration / DurationScale);

                HideWarnings();
                FireBeams();
                yield return new WaitForSeconds(activeDuration / DurationScale);
                HideBeams();

                if (i < repeatCount - 1)
                    yield return new WaitForSeconds(intervalBetweenShots / DurationScale);
            }

            ReturnOrigins();
            yield return new WaitForSeconds(returnDuration / DurationScale);
        }

        private void CacheOrigins()
        {
            originStates.Clear();
            foreach (Transform origin in Boss.GetPatternOrigins())
            {
                if (origin == null) continue;
                originStates.Add(new TransformState(origin, origin.position, origin.rotation));
            }
        }
        private void MoveToNextPositions()
        {
            Vector3 playerPosition = Owner.Target.transform.position;

            float xOffset = Random.Range(5f, 8f);
            if (Random.value < 0.5f)
                xOffset *= -1f;

            float yOffset = Random.Range(-8f, 8f);

            Vector3 mainPosition = playerPosition + new Vector3(
                xOffset,
                yOffset,
                0f
            );

            lastMainPosition = mainPosition;
            hasLastMainPosition = true;

            for (int i = 0; i < originStates.Count; i++)
            {
                Transform origin = originStates[i].Transform;
                if (origin == null) continue;

                Vector3 destination = i == 0
                    ? mainPosition
                    : playerPosition;

                destination = Boss.Arena != null
                    ? Boss.Arena.Clamp(destination, beamWidth)
                    : destination;

                destination.z = origin.position.z;

                origin.DOKill();
                origin.DOMove(destination, moveDuration / DurationScale)
                    .SetEase(Ease.InOutCubic);
            }
        }

        private Vector3 GetDifferentPosition()
        {
            Vector3 center = Boss.ArenaCenter;
            Vector2 heightRate = Boss.IsPhaseTwo
                ? phaseTwoHeightRate
                : phaseOneHeightRate;
            Vector3 candidate = center;

            for (int attempt = 0; attempt < 12; attempt++)
            {
                candidate.x = center.x + Random.Range(
                    -Boss.ArenaHalfWidth * horizontalRangeRate,
                    Boss.ArenaHalfWidth * horizontalRangeRate
                );
                candidate.y = center.y + Boss.ArenaHalfHeight * Random.Range(
                    Mathf.Min(heightRate.x, heightRate.y),
                    Mathf.Max(heightRate.x, heightRate.y)
                );

                if (!hasLastMainPosition ||
                    Vector2.Distance(candidate, lastMainPosition) >= minimumPositionDistance)
                    break;
            }

            if (hasLastMainPosition &&
                Vector2.Distance(candidate, lastMainPosition) < minimumPositionDistance)
            {
                float direction = lastMainPosition.x >= center.x ? -1f : 1f;
                candidate.x = Mathf.Clamp(
                    lastMainPosition.x + minimumPositionDistance * direction,
                    center.x - Boss.ArenaHalfWidth * horizontalRangeRate,
                    center.x + Boss.ArenaHalfWidth * horizontalRangeRate
                );
            }

            return candidate;
        }

        private void BuildShots()
        {
            currentShots.Clear();
            Vector3 targetPosition = Boss.Target != null
                ? Boss.Target.position
                : Boss.ArenaCenter + Vector3.down;
            float range = Mathf.Sqrt(
                Boss.ArenaHalfWidth * Boss.ArenaHalfWidth +
                Boss.ArenaHalfHeight * Boss.ArenaHalfHeight
            ) * Mathf.Max(1f, beamRangeMultiplier);

            int beamCount = Boss.IsPhaseTwo
                ? Mathf.Max(1, phaseTwoBeamCount)
                : Mathf.Max(1, phaseOneBeamCount);
            float spread = Boss.IsPhaseTwo
                ? phaseTwoBeamSpread
                : phaseOneBeamSpread;

            foreach (TransformState state in originStates)
            {
                if (state.Transform == null) continue;
                Vector3 start = state.Transform.position;
                Vector2 aimDirection = targetPosition - start;
                if (aimDirection.sqrMagnitude <= Mathf.Epsilon) aimDirection = Vector2.down;
                float aimAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

                for (int i = 0; i < beamCount; i++)
                {
                    float rate = beamCount == 1 ? 0.5f : i / (float)(beamCount - 1);
                    float angle = aimAngle + Mathf.Lerp(-spread * 0.5f, spread * 0.5f, rate);
                    currentShots.Add(new BeamShot(
                        BuildReflectedPath(start, Direction(angle), range)
                    ));
                }
            }

            EnsureBeamVisuals(currentShots.Count);
        }

        private Vector3[] BuildReflectedPath(Vector3 start, Vector2 direction, float range)
        {
            List<Vector3> points = new List<Vector3> { start };
            Vector2 rayOrigin = start;
            Vector2 rayDirection = direction.normalized;
            float remainingDistance = range;
            float skin = Mathf.Max(0.001f, reflectionSkin);

            while (remainingDistance > skin)
            {
                RaycastHit2D layerHit = Physics2D.Raycast(
                    rayOrigin,
                    rayDirection,
                    remainingDistance,
                    reflectionLayer
                );
                bool hasArenaHit = TryGetArenaHit(
                    rayOrigin,
                    rayDirection,
                    remainingDistance,
                    out float arenaDistance,
                    out Vector2 arenaNormal
                );

                bool hasLayerHit = layerHit.collider != null;
                bool useArenaHit = hasArenaHit &&
                    (!hasLayerHit || arenaDistance <= layerHit.distance);

                if (!hasLayerHit && !hasArenaHit)
                {
                    points.Add(rayOrigin + rayDirection * remainingDistance);
                    break;
                }

                float hitDistance = useArenaHit ? arenaDistance : layerHit.distance;
                Vector2 hitNormal = useArenaHit ? arenaNormal : layerHit.normal;
                Vector2 hitPoint = rayOrigin + rayDirection * hitDistance;
                points.Add(hitPoint);
                remainingDistance -= Mathf.Max(hitDistance, skin);
                if (remainingDistance <= skin) break;

                rayDirection = Vector2.Reflect(rayDirection, hitNormal).normalized;
                rayOrigin = hitPoint + rayDirection * skin;
                remainingDistance -= skin;
            }

            if (points.Count < 2)
                points.Add(start + (Vector3)direction.normalized * range);
            return points.ToArray();
        }

        private bool TryGetArenaHit(
            Vector2 origin,
            Vector2 direction,
            float maximumDistance,
            out float distance,
            out Vector2 normal)
        {
            Vector3 center = Boss.ArenaCenter;
            float minX = center.x - Boss.ArenaHalfWidth;
            float maxX = center.x + Boss.ArenaHalfWidth;
            float minY = center.y - Boss.ArenaHalfHeight;
            float maxY = center.y + Boss.ArenaHalfHeight;
            float minimumDistance = Mathf.Max(0.001f, reflectionSkin);

            float xDistance = float.PositiveInfinity;
            float yDistance = float.PositiveInfinity;
            Vector2 xNormal = Vector2.zero;
            Vector2 yNormal = Vector2.zero;

            if (direction.x > Mathf.Epsilon)
            {
                xDistance = (maxX - origin.x) / direction.x;
                xNormal = Vector2.left;
            }
            else if (direction.x < -Mathf.Epsilon)
            {
                xDistance = (minX - origin.x) / direction.x;
                xNormal = Vector2.right;
            }

            if (direction.y > Mathf.Epsilon)
            {
                yDistance = (maxY - origin.y) / direction.y;
                yNormal = Vector2.down;
            }
            else if (direction.y < -Mathf.Epsilon)
            {
                yDistance = (minY - origin.y) / direction.y;
                yNormal = Vector2.up;
            }

            bool validX = xDistance >= -minimumDistance && xDistance <= maximumDistance;
            bool validY = yDistance >= -minimumDistance && yDistance <= maximumDistance;
            if (!validX && !validY)
            {
                distance = 0f;
                normal = Vector2.zero;
                return false;
            }

            if (validX && validY && Mathf.Abs(xDistance - yDistance) <= minimumDistance)
            {
                distance = Mathf.Max(0f, Mathf.Min(xDistance, yDistance));
                normal = (xNormal + yNormal).normalized;
                return true;
            }

            if (validX && (!validY || xDistance < yDistance))
            {
                distance = Mathf.Max(0f, xDistance);
                normal = xNormal;
                return true;
            }

            distance = Mathf.Max(0f, yDistance);
            normal = yNormal;
            return true;
        }

        private void ShowWarnings()
        {
            for (int i = 0; i < currentShots.Count; i++)
            {
                BeamShot shot = currentShots[i];
                warningLines[i].Show(
                    shot.Points,
                    chargeDuration * 0.72f / DurationScale
                );
            }
        }

        private void FireBeams()
        {
            float damage = Boss.IsPhaseTwo ? phaseTwoDamage : phaseOneDamage;
            float castDuration = activeDuration / DurationScale;
            int requiredCasterCount = 0;
            foreach (BeamShot shot in currentShots)
                requiredCasterCount += Mathf.Max(0, shot.Points.Length - 1);
            EnsureBeamCasters(requiredCasterCount);

            int casterIndex = 0;

            for (int i = 0; i < currentShots.Count; i++)
            {
                BeamShot shot = currentShots[i];
                List<BeamSegment> segments = new List<BeamSegment>();

                LineRenderer line = beamLines[i];
                DOTween.Kill(line);
                line.positionCount = shot.Points.Length;
                line.SetPositions(shot.Points);
                line.widthMultiplier = beamWidth * Mathf.Max(1f, flashWidthMultiplier);
                SetLineColor(line, Color.white);
                line.enabled = true;

                for (int pointIndex = 0; pointIndex < shot.Points.Length - 1; pointIndex++)
                {
                    Vector3 segmentStart = shot.Points[pointIndex];
                    Vector3 segmentEnd = shot.Points[pointIndex + 1];
                    Vector2 direction = segmentEnd - segmentStart;
                    float length = direction.magnitude;
                    if (length <= Mathf.Epsilon) continue;

                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    Vector3 center = Vector3.Lerp(segmentStart, segmentEnd, 0.5f);
                    DamageCaster caster = beamCasters[casterIndex++];
                    float flashWidth = beamWidth * Mathf.Max(1f, flashWidthMultiplier);
                    caster.ConfigureBox(new Vector2(length, flashWidth), Boss.PlayerLayer);
                    caster.SetWorldPose(center, angle);
                    caster.EnableCasting(
                        new DamageData(damage, DamageType.Beam),
                        castDuration
                    );
                    segments.Add(new BeamSegment(caster, length));
                }
                AnimateBeam(line, segments, castDuration);
                Boss.AttackImpact(shot.Points[0]);
            }
        }

        private void AnimateBeam(
            LineRenderer line,
            IReadOnlyList<BeamSegment> segments,
            float duration)
        {
            float actualDuration = Mathf.Max(0.02f, duration);
            float actualFlashDuration = Mathf.Clamp(
                flashDuration / DurationScale,
                0.01f,
                actualDuration
            );
            float startWidth = beamWidth * Mathf.Max(1f, flashWidthMultiplier);
            float currentWidth = startWidth;
            float currentAlpha = 1f;

            Sequence sequence = DOTween.Sequence().SetTarget(line);
            sequence.AppendInterval(actualFlashDuration);
            sequence.AppendCallback(() => SetLineColor(line, beamColor));
            sequence.Append(DOTween.To(
                    () => currentWidth,
                    value =>
                    {
                        currentWidth = value;
                        line.widthMultiplier = value;
                        foreach (BeamSegment segment in segments)
                            segment.Caster.SetSize(new Vector2(segment.Length, value));
                    },
                    0f,
                    Mathf.Max(0.01f, actualDuration - actualFlashDuration))
                .SetEase(Ease.InQuad));
            sequence.Join(DOTween.To(
                    () => currentAlpha,
                    value =>
                    {
                        currentAlpha = value;
                        Color color = beamColor;
                        color.a *= value;
                        SetLineColor(line, color);
                    },
                    0f,
                    Mathf.Max(0.01f, actualDuration - actualFlashDuration))
                .SetEase(Ease.InQuad));
            sequence.OnComplete(() => line.enabled = false);
        }

        private static void SetLineColor(LineRenderer line, Color color)
        {
            line.startColor = color;
            line.endColor = color;
        }

        private void EnsureBeamVisuals(int count)
        {
            while (warningLines.Count < count)
            {
                int index = warningLines.Count + 1;
                GameObject warningObject = new GameObject($"Reflection Warning {index}");
                warningObject.transform.SetParent(transform, false);
                LineRenderer warningRenderer = CreateLineRenderer(
                    warningObject,
                    Mathf.Max(0.025f, beamWidth * 0.18f),
                    warningColor,
                    false
                );
                warningRenderer.enabled = false;
                warningLines.Add(warningObject.AddComponent<MoonTelegraphLine>());

                GameObject beamObject = new GameObject($"Reflection Beam {index}");
                beamObject.transform.SetParent(transform, false);
                LineRenderer beamRenderer = CreateLineRenderer(
                    beamObject,
                    beamWidth,
                    beamColor,
                    true
                );
                beamRenderer.enabled = false;
                beamLines.Add(beamRenderer);
            }
        }

        private void EnsureBeamCasters(int count)
        {
            while (beamCasters.Count < count)
            {
                GameObject casterObject = new GameObject(
                    $"Reflection Beam Caster {beamCasters.Count + 1}"
                );
                casterObject.transform.SetParent(transform, false);
                beamCasters.Add(casterObject.AddComponent<DamageCaster>());
            }
        }

        private static LineRenderer CreateLineRenderer(
            GameObject owner,
            float width,
            Color color,
            bool useLaserMaterial)
        {
            LineRenderer line = owner.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.widthMultiplier = width;
            line.startColor = color;
            line.endColor = color;
            line.numCapVertices = 8;
            line.numCornerVertices = 4;
            line.textureMode = LineTextureMode.Tile;

            if (useLaserMaterial)
            {
                Material laserMaterial = Resources.Load<Material>("ODKLaser");
                if (laserMaterial != null) line.sharedMaterial = laserMaterial;
                else
                {
                    Shader laserShader = Shader.Find("ODK/Laser");
                    if (laserShader != null) line.material = new Material(laserShader);
                }
            }
            else
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null) line.material = new Material(shader);
            }
            return line;
        }

        private void HideWarnings()
        {
            foreach (MoonTelegraphLine line in warningLines)
            {
                if (line != null) line.Hide();
            }
        }

        private void HideBeams()
        {
            foreach (LineRenderer line in beamLines)
            {
                if (line == null) continue;
                DOTween.Kill(line);
                line.widthMultiplier = beamWidth;
                SetLineColor(line, beamColor);
                line.enabled = false;
            }
            foreach (DamageCaster caster in beamCasters)
            {
                if (caster != null) caster.DisableCasting();
            }
        }

        private void ReturnOrigins()
        {
            foreach (TransformState state in originStates)
            {
                if (state.Transform == null) continue;
                state.Transform.DOKill();
                state.Transform.DOMove(state.StartPosition, returnDuration / DurationScale)
                    .SetEase(Ease.InOutSine);
                state.Transform.DORotateQuaternion(
                    state.StartRotation,
                    returnDuration / DurationScale
                ).SetEase(Ease.InOutSine);
            }
        }

        protected override void OnCancel()
        {
            HideWarnings();
            HideBeams();
            foreach (TransformState state in originStates)
            {
                if (state.Transform == null) continue;
                state.Transform.DOKill();
                state.Transform.SetPositionAndRotation(state.StartPosition, state.StartRotation);
            }
            originStates.Clear();
            currentShots.Clear();
        }

        private readonly struct BeamShot
        {
            public readonly Vector3[] Points;

            public BeamShot(Vector3[] points)
            {
                Points = points;
            }
        }

        private readonly struct BeamSegment
        {
            public readonly DamageCaster Caster;
            public readonly float Length;

            public BeamSegment(DamageCaster caster, float length)
            {
                Caster = caster;
                Length = length;
            }
        }

        private static Vector2 Direction(float angle)
        {
            float radians = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        private readonly struct TransformState
        {
            public readonly Transform Transform;
            public readonly Vector3 StartPosition;
            public readonly Quaternion StartRotation;

            public TransformState(
                Transform transform,
                Vector3 startPosition,
                Quaternion startRotation)
            {
                Transform = transform;
                StartPosition = startPosition;
                StartRotation = startRotation;
            }
        }
    }
}
