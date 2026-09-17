using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    public class MoonReflectionAttack : MoonSkill
    {
        [Header("Movement")]
        [SerializeField] private float phaseOneRiseHeight = 4f;
        [SerializeField] private float phaseTwoBackDistance = 2f;
        [SerializeField] private float moveDuration = 0.65f;
        [SerializeField] private float returnDuration = 0.55f;

        [Header("Charge")]
        [SerializeField] private float chargeDuration = 1.2f;
        [SerializeField] private float activeDuration = 0.25f;
        [SerializeField] private float attackRange = 16f;

        [Header("Phase One Fan")]
        [SerializeField] private int narrowFanCount = 4;
        [SerializeField] private float narrowFanAngle = 18f;
        [SerializeField] private float narrowFanSpread = 105f;
        [SerializeField] private float phaseOneDamage = 38f;

        [Header("Phase Two Fan")]
        [SerializeField] private float wideFanAngle = 125f;
        [SerializeField] private float phaseTwoDamage = 65f;

        [Header("References")]
        [SerializeField] private DamageCaster fanCaster;

        private readonly List<TransformState> originStates = new List<TransformState>();
        private readonly List<MoonTelegraphLine> warningLines = new List<MoonTelegraphLine>();

        public override bool CanUseSkill(GameObject target = null)
        {
            return Boss != null && Boss.Target != null && !Boss.IsDead;
        }

        protected override void OnMoonInitialize()
        {
            if (fanCaster == null)
            {
                GameObject casterObject = new GameObject("Moon Reflection Caster");
                casterObject.transform.SetParent(transform, false);
                fanCaster = casterObject.AddComponent<DamageCaster>();
            }
        }

        protected override IEnumerator ExecuteMoon(GameObject target)
        {
            CacheOrigins();
            MoveToAttackPositions();
            yield return new WaitForSeconds(moveDuration / DurationScale);

            List<float> castAngles = BuildCastAngles();
            ShowWarnings(castAngles);
            Boss.AttackReady(Boss.transform.position);
            yield return new WaitForSeconds(chargeDuration / DurationScale);

            HideWarnings();
            CastFans(castAngles);
            yield return new WaitForSeconds(activeDuration / DurationScale);

            ReturnOrigins();
            yield return new WaitForSeconds(returnDuration / DurationScale);
        }

        private void CacheOrigins()
        {
            originStates.Clear();
            foreach (Transform origin in Boss.GetPatternOrigins())
            {
                originStates.Add(new TransformState(
                    origin,
                    origin.position,
                    origin.rotation
                ));
            }
        }

        private void MoveToAttackPositions()
        {
            foreach (TransformState state in originStates)
            {
                if (state.Transform == null) continue;

                Vector3 targetPosition = state.StartPosition;
                if (Boss.IsPhaseTwo)
                {
                    float targetX = Boss.Target != null
                        ? Boss.Target.position.x
                        : Boss.ArenaCenter.x;
                    float direction = Mathf.Sign(state.StartPosition.x - targetX);
                    if (Mathf.Approximately(direction, 0f)) direction = 1f;
                    targetPosition += Vector3.right * direction * phaseTwoBackDistance;
                }
                else
                {
                    targetPosition += Vector3.up * phaseOneRiseHeight;
                }

                state.Transform.DOKill();
                state.Transform.DOMove(targetPosition, moveDuration / DurationScale)
                    .SetEase(Ease.OutCubic);
            }
        }

        private List<float> BuildCastAngles()
        {
            List<float> angles = new List<float>();
            if (Boss.IsPhaseTwo)
            {
                angles.Add(GetAimAngle(Boss.transform.position));
                return angles;
            }

            int count = Mathf.Max(1, narrowFanCount);
            float centerAngle = GetAimAngle(Boss.transform.position);
            for (int i = 0; i < count; i++)
            {
                float rate = count == 1 ? 0.5f : i / (float)(count - 1);
                angles.Add(centerAngle + Mathf.Lerp(
                    -narrowFanSpread * 0.5f,
                    narrowFanSpread * 0.5f,
                    rate
                ));
            }
            return angles;
        }

        private float GetAimAngle(Vector3 origin)
        {
            Vector2 direction = Boss.Target != null
                ? Boss.Target.position - origin
                : Vector2.down;
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }

        private void CastFans(List<float> angles)
        {
            if (fanCaster == null) return;

            float width = Boss.IsPhaseTwo ? wideFanAngle : narrowFanAngle;
            float damage = Boss.IsPhaseTwo ? phaseTwoDamage : phaseOneDamage;
            fanCaster.ConfigureSector(attackRange, width, Boss.PlayerLayer);

            foreach (TransformState state in originStates)
            {
                if (state.Transform == null) continue;
                foreach (float angle in angles)
                {
                    fanCaster.SetWorldPose(state.Transform.position, angle);
                    fanCaster.Cast(new DamageData(damage, DamageType.Beam));
                }
                Boss.AttackImpact(state.Transform.position);
            }
        }

        private void ShowWarnings(List<float> angles)
        {
            int requiredCount = originStates.Count * angles.Count;
            EnsureWarningLines(requiredCount);

            float width = Boss.IsPhaseTwo ? wideFanAngle : narrowFanAngle;
            int lineIndex = 0;
            foreach (TransformState state in originStates)
            {
                if (state.Transform == null) continue;
                foreach (float angle in angles)
                {
                    MoonTelegraphLine line = warningLines[lineIndex++];
                    Vector3 origin = state.Transform.position;
                    line.Show(
                        new[]
                        {
                            origin,
                            origin + Direction(angle - width * 0.5f) * attackRange,
                            origin,
                            origin + Direction(angle + width * 0.5f) * attackRange
                        },
                        chargeDuration * 0.65f / DurationScale
                    );
                }
            }
        }

        private void EnsureWarningLines(int count)
        {
            while (warningLines.Count < count)
            {
                GameObject lineObject = new GameObject($"Reflection Warning {warningLines.Count + 1}");
                lineObject.transform.SetParent(transform, false);
                LineRenderer line = lineObject.AddComponent<LineRenderer>();
                line.widthMultiplier = 0.06f;
                line.startColor = new Color(1f, 0.9f, 0.45f, 0.25f);
                line.endColor = new Color(1f, 0.9f, 0.45f, 0.85f);
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null) line.material = new Material(shader);
                line.enabled = false;
                warningLines.Add(lineObject.AddComponent<MoonTelegraphLine>());
            }
        }

        private void HideWarnings()
        {
            foreach (MoonTelegraphLine line in warningLines)
            {
                if (line != null) line.Hide();
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

        private static Vector3 Direction(float angle)
        {
            float radians = angle * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        protected override void OnCancel()
        {
            HideWarnings();
            foreach (TransformState state in originStates)
            {
                if (state.Transform == null) continue;
                state.Transform.DOKill();
                state.Transform.SetPositionAndRotation(state.StartPosition, state.StartRotation);
            }
            originStates.Clear();
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
