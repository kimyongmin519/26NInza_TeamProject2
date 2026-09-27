using System.Collections;
using DG.Tweening;
using GGMLib.ObjectPool.Runtime;
using Member.ODK.Scripts.Enemys.Bosses;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    [RequireComponent(typeof(LineRenderer), typeof(DamageCaster))]
    public class LostSoulSlashBeam : AbstractMonoPoolable
    {
        [SerializeField] private LineRenderer line;
        [SerializeField] private DamageCaster caster;

        private Sequence visualSequence;
        private Vector3 rotationCenter;
        private Vector2 currentDirection;
        private Vector2 initialDirection;
        private float halfLength;
        private Tween rotationTween;
        private Coroutine fireRoutine;
        private Material defaultMaterial;

        public override void ResetItem()
        {
            StopBeam();
        }

        private void StopBeam()
        {
            if (fireRoutine != null) StopCoroutine(fireRoutine);
            fireRoutine = null;
            if (line != null) DOTween.Kill(line);
            rotationTween?.Kill();
            rotationTween = null;
            visualSequence?.Kill();
            visualSequence = null;
            if (caster != null) caster.DisableCasting();
        }

        public void Initialize(
            LostSoul owner,
            Vector3 origin,
            Vector2 direction,
            float length,
            float width,
            float warningDuration,
            float activeDuration,
            float damage,
            Color color,
            float preFireRotationDegrees = 0f)
        {
            if (line == null) line = GetComponent<LineRenderer>();
            if (caster == null) caster = GetComponent<DamageCaster>();
            if (line == null || caster == null)
            {
                ODKPool.Despawn(this);
                return;
            }
            StopBeam();
            if (defaultMaterial == null) defaultMaterial = line.sharedMaterial;
            else line.sharedMaterial = defaultMaterial;

            Vector2 normalized = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
            Vector3 end = origin + (Vector3)normalized * length;
            rotationCenter = Vector3.Lerp(origin, end, 0.5f);
            currentDirection = normalized;
            initialDirection = normalized;
            halfLength = length * 0.5f;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, origin);
            line.SetPosition(1, end);
            line.widthMultiplier = Mathf.Max(0.025f, width * 0.16f);
            line.startColor = new Color(1f, 1f, 1f, 0.82f);
            line.endColor = new Color(1f, 1f, 1f, 0.45f);
            line.numCapVertices = 8;
            line.textureMode = LineTextureMode.Tile;
            line.sortingOrder = 60;

            float angle = Mathf.Atan2(normalized.y, normalized.x) * Mathf.Rad2Deg;
            caster.ConfigureBox(new Vector2(length, width), owner.PlayerLayer);
            caster.SetWorldPose(Vector3.Lerp(origin, end, 0.5f), angle);
            owner.PlayBeamWarningFeedback();
            StartPreFireRotation(preFireRotationDegrees, warningDuration);
            fireRoutine = StartCoroutine(FireRoutine(owner, width, warningDuration, activeDuration, damage, color));
        }

        private void StartPreFireRotation(float degrees, float warningDuration)
        {
            if (Mathf.Approximately(degrees, 0f)) return;
            float rotationDuration = Mathf.Max(0.02f, warningDuration * 0.72f);
            rotationTween = DOTween.To(
                    () => 0f,
                    angle => SetRotation(angle),
                    degrees,
                    rotationDuration
                )
                .SetEase(Ease.OutQuart)
                .SetTarget(this);
        }

        private void SetRotation(float angleOffset)
        {
            if (line == null || caster == null) return;
            currentDirection = Quaternion.Euler(0f, 0f, angleOffset) * initialDirection;
            Vector3 start = rotationCenter - (Vector3)currentDirection * halfLength;
            Vector3 end = rotationCenter + (Vector3)currentDirection * halfLength;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            float angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
            caster.SetWorldPose(rotationCenter, angle);
        }

        private IEnumerator FireRoutine(
            LostSoul owner,
            float width,
            float warningDuration,
            float activeDuration,
            float damage,
            Color color)
        {
            float warningWidth = line.widthMultiplier;
            DOTween.To(() => line.widthMultiplier, value => line.widthMultiplier = value,
                    warningWidth * 0.42f, Mathf.Max(0.06f, warningDuration * 0.28f))
                .SetLoops(-1, LoopType.Yoyo)
                .SetTarget(line);
            yield return new WaitForSeconds(Mathf.Max(0f, warningDuration));
            DOTween.Kill(line);
            rotationTween?.Complete();
            owner.PlayBeamFireFeedback();
            owner.ShakeCamera(0.28f);

            Material laserMaterial = Resources.Load<Material>("ODKLaser");
            if (laserMaterial != null) line.sharedMaterial = laserMaterial;
            else
            {
                Shader laserShader = Shader.Find("ODK/Laser");
                if (laserShader != null) line.material = new Material(laserShader);
            }

            line.startColor = Color.white;
            line.endColor = Color.white;
            line.widthMultiplier = width * 1.8f;
            float length = Vector3.Distance(line.GetPosition(0), line.GetPosition(1));
            caster.SetSize(new Vector2(length, width * 1.8f));
            caster.EnableCasting(new DamageData(damage, DamageType.Beam), activeDuration);
            owner.AttackImpact(Vector3.Lerp(line.GetPosition(0), line.GetPosition(1), 0.5f));

            float currentWidth = line.widthMultiplier;
            float alpha = 1f;
            visualSequence = DOTween.Sequence().SetTarget(this);
            visualSequence.AppendInterval(Mathf.Min(0.05f, activeDuration * 0.25f));
            visualSequence.AppendCallback(() => SetColor(color));
            visualSequence.Append(DOTween.To(
                () => currentWidth,
                value =>
                {
                    currentWidth = value;
                    line.widthMultiplier = value;
                    caster.SetSize(new Vector2(length, value));
                },
                0f,
                Mathf.Max(0.02f, activeDuration - 0.05f)
            ).SetEase(Ease.InQuad));
            visualSequence.Join(DOTween.To(
                () => alpha,
                value =>
                {
                    alpha = value;
                    Color faded = color;
                    faded.a *= value;
                    SetColor(faded);
                },
                0f,
                Mathf.Max(0.02f, activeDuration - 0.05f)
            ));
            yield return visualSequence.WaitForCompletion();
            fireRoutine = null;
            visualSequence = null;
            caster.DisableCasting();
            ODKPool.Despawn(this);
        }

        private void SetColor(Color color)
        {
            line.startColor = color;
            line.endColor = color;
        }

        private void OnDisable()
        {
            if (line != null) DOTween.Kill(line);
            rotationTween?.Kill();
            visualSequence?.Kill();
            fireRoutine = null;
        }

        private void OnDestroy()
        {
            DOTween.Kill(line);
            rotationTween?.Kill();
            visualSequence?.Kill();
        }
    }
}
