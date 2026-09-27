using System.Collections;
using DG.Tweening;
using GGMLib.ObjectPool.Runtime;
using Member.ODK.Scripts.Enemys.Bosses;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    public class MoonLaserShot : AbstractMonoPoolable
    {
        private LineRenderer line;
        private DamageCaster caster;
        private Sequence visualSequence;
        private LayerMask targetLayer;
        private Material warningMaterial;
        private Coroutine fireRoutine;

        public override void ResetItem()
        {
            StopShot();
            if (line == null) line = GetComponent<LineRenderer>();
            if (line != null)
            {
                line.widthMultiplier = 0f;
                line.enabled = true;
            }
        }

        private void StopShot()
        {
            if (fireRoutine != null) StopCoroutine(fireRoutine);
            fireRoutine = null;
            if (line != null) DOTween.Kill(line);
            visualSequence?.Kill();
            visualSequence = null;
            caster?.DisableCasting();
        }

        public void Initialize(
            MoonBoss owner,
            Vector3 origin,
            Vector2 direction,
            float length,
            float width,
            float warningDuration,
            float activeDuration,
            float damage,
            LayerMask playerLayer,
            Color color)
        {
            Build(
                origin,
                direction,
                length,
                width,
                warningDuration,
                activeDuration,
                damage,
                playerLayer,
                color,
                owner
            );
        }

        private void Build(
            Vector3 origin,
            Vector2 direction,
            float length,
            float width,
            float warningDuration,
            float activeDuration,
            float damage,
            LayerMask playerLayer,
            Color color,
            MoonBoss owner)
        {
            Vector2 normalizedDirection = direction.sqrMagnitude > 0.001f
                ? direction.normalized
                : Vector2.down;
            Vector3 end = origin + (Vector3)normalizedDirection * length;

            line = GetComponent<LineRenderer>();
            caster = GetComponent<DamageCaster>();
            if (line == null || caster == null)
            {
                ODKPool.Despawn(this);
                return;
            }
            StopShot();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, origin);
            line.SetPosition(1, end);
            line.widthMultiplier = Mathf.Max(0.02f, width * 0.16f);
            line.startColor = new Color(1f, 1f, 1f, 0.9f);
            line.endColor = new Color(1f, 1f, 1f, 0.5f);
            line.numCapVertices = 8;
            line.textureMode = LineTextureMode.Tile;
            line.sortingOrder = 60;
            if (warningMaterial == null)
            {
                Shader warningShader = Shader.Find("Sprites/Default");
                if (warningShader != null) warningMaterial = new Material(warningShader);
            }
            if (warningMaterial != null) line.sharedMaterial = warningMaterial;

            targetLayer = playerLayer;
            float angle = Mathf.Atan2(normalizedDirection.y, normalizedDirection.x) * Mathf.Rad2Deg;
            caster.ConfigureBox(new Vector2(length, width), playerLayer);
            caster.SetWorldPose(Vector3.Lerp(origin, end, 0.5f), angle);
            fireRoutine = StartCoroutine(FireRoutine(width, warningDuration, activeDuration, damage, color, owner));
        }

        private IEnumerator FireRoutine(
            float width,
            float warningDuration,
            float activeDuration,
            float damage,
            Color color,
            MoonBoss owner)
        {
            float warningWidth = line.widthMultiplier;
            line.DOTweenWidth(warningWidth * 0.45f, Mathf.Max(0.06f, warningDuration * 0.28f))
                .SetLoops(-1, LoopType.Yoyo);
            yield return new WaitForSeconds(Mathf.Max(0f, warningDuration));
            DOTween.Kill(line);
            owner?.PlayLaserFeedback(line.GetPosition(0));
            owner?.ShakeCamera(0.3f);

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
            caster.ConfigureBox(
                new Vector2(
                    Vector3.Distance(line.GetPosition(0), line.GetPosition(1)),
                    width * 1.8f
                ),
                targetLayer
            );
            caster.EnableCasting(new DamageData(damage, DamageType.Beam), activeDuration);

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
                    caster.SetSize(new Vector2(
                        Vector3.Distance(line.GetPosition(0), line.GetPosition(1)),
                        value
                    ));
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
            visualSequence?.Kill();
            visualSequence = null;
            fireRoutine = null;
        }

        private void OnDestroy()
        {
            if (line != null) DOTween.Kill(line);
            visualSequence?.Kill();
            if (warningMaterial != null) Destroy(warningMaterial);
        }
    }

    internal static class MoonLineRendererTweenExtensions
    {
        public static Tween DOTweenWidth(this LineRenderer line, float endValue, float duration)
        {
            return DOTween.To(
                () => line.widthMultiplier,
                value => line.widthMultiplier = value,
                endValue,
                duration
            ).SetTarget(line);
        }
    }
}
