using System.Collections;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    public class MoonMiniDashAttack : MoonSkill
    {
        [Header("Shrink")]
        [SerializeField, Range(0.1f, 1f)] private float shrinkScale = 0.42f;
        [SerializeField] private float shrinkDuration = 0.32f;

        [Header("Dash")]
        [SerializeField] private int dashCount = 8;
        [SerializeField] private float dashPeriod = 0.58f;
        [SerializeField] private float dashDuration = 0.16f;
        [SerializeField] private float dashAcceleration = 90f;
        [SerializeField] private float dashMaxSpeed = 35f;
        [SerializeField] private float contactRadius = 0.85f;
        [SerializeField] private float contactDamage = 38f;

        [Header("Phase Two Light")]
        [SerializeField] private int lightEveryDash = 2;
        [SerializeField] private float lightWarningDuration = 0.28f;
        [SerializeField] private float lightActiveDuration = 0.16f;
        [SerializeField] private float lightWidth = 0.3f;
        [SerializeField] private float lightDamage = 30f;
        [SerializeField] private Color lightColor = new Color(0.65f, 0.82f, 1f, 1f);

        private DamageCaster contactCaster;
        private Vector3 originPosition;
        private Vector3 originScale;
        private bool hasOrigin;
        protected override bool UsesAmbientFloating => false;

        public override bool CanUseSkill(GameObject target = null)
        {
            return Boss != null && Boss.Target != null && !Boss.IsDead;
        }

        protected override void OnMoonInitialize()
        {
            contactCaster = CreateCaster("Mini Dash Caster");
        }

        protected override IEnumerator ExecuteMoon(GameObject target)
        {
            originPosition = Boss.transform.position;
            originScale = Boss.transform.localScale;
            hasOrigin = true;
            Boss.transform.DOKill();
            Boss.PlayShrinkFeedback(Boss.transform.position);
            yield return Boss.transform.DOScale(originScale * shrinkScale, shrinkDuration / DurationScale)
                .SetEase(Ease.InBack)
                .WaitForCompletion();

            int count = Mathf.Max(1, dashCount);
            for (int i = 0; i < count && !Boss.IsDead; i++)
            {
                float readyTime = Mathf.Max(0f, dashPeriod - dashDuration);
                Boss.AttackReady(Boss.Target != null ? Boss.Target.position : Boss.transform.position);
                if (readyTime > 0f)
                    yield return new WaitForSeconds(readyTime / DurationScale);

                if (Boss.IsPhaseTwo && lightEveryDash > 0 && (i + 1) % lightEveryDash == 0)
                    FireLight();

                yield return MoveWithAcceleration(
                    dashDuration / DurationScale,
                    dashAcceleration * DurationScale,
                    dashMaxSpeed * DurationScale,
                    true
                );
            }

            contactCaster.DisableCasting();
            Boss.transform.DOKill();
            Sequence restore = DOTween.Sequence();
            restore.Join(Boss.transform.DOScale(originScale, 0.35f / DurationScale).SetEase(Ease.OutBack));
            restore.Join(Boss.transform.DOMove(originPosition, 0.45f / DurationScale).SetEase(Ease.InOutSine));
            yield return restore.WaitForCompletion();
            hasOrigin = false;
        }

        private IEnumerator MoveWithAcceleration(
            float duration,
            float acceleration,
            float maxSpeed,
            bool damagingDash)
        {
            Vector2 direction = Boss.Target != null
                ? ((Vector2)Boss.Target.position - (Vector2)Boss.transform.position).normalized
                : Vector2.right;
            Vector2 velocity = direction * maxSpeed;
            if (damagingDash)
            {
                contactCaster.transform.position = Boss.transform.position;
                contactCaster.ConfigureCircle(contactRadius, Boss.PlayerLayer);
                contactCaster.EnableCasting(
                    new DamageData(contactDamage, DamageType.Melee),
                    duration
                );
                Boss.PlayDashFeedback(Boss.transform.position);
            }

            float elapsed = 0f;
            while (elapsed < duration && !Boss.IsDead)
            {
                float delta = Time.deltaTime;
                direction = Boss.Target != null
                    ? ((Vector2)Boss.Target.position - (Vector2)Boss.transform.position).normalized
                    : (velocity.sqrMagnitude > Mathf.Epsilon ? velocity.normalized : Vector2.right);
                Vector2 targetVelocity = direction * maxSpeed;
                velocity = Vector2.MoveTowards(velocity, targetVelocity, acceleration * delta);

                Vector3 next = Boss.transform.position + (Vector3)velocity * delta;
                if (Boss.Arena != null)
                {
                    Vector3 clamped = Boss.Arena.Clamp(next, contactRadius);
                    if (!Mathf.Approximately(clamped.x, next.x)) velocity.x = 0f;
                    if (!Mathf.Approximately(clamped.y, next.y)) velocity.y = 0f;
                    next = clamped;
                }
                next.z = Boss.transform.position.z;
                Boss.transform.position = next;
                contactCaster.transform.position = next;
                elapsed += delta;
                yield return null;
            }

            if (damagingDash)
            {
                contactCaster.DisableCasting();
                Boss.AttackImpact(Boss.transform.position);
                Boss.ShakeCamera(0.48f);
            }
        }

        private void FireLight()
        {
            if (Boss.Target == null) return;
            Vector2 direction = Boss.Target.position - Boss.transform.position;
            float length = Mathf.Sqrt(
                Boss.ArenaHalfWidth * Boss.ArenaHalfWidth +
                Boss.ArenaHalfHeight * Boss.ArenaHalfHeight
            ) * 2.5f;
            Boss.SpawnLaser(
                Boss.transform.position,
                direction,
                length,
                lightWidth,
                lightWarningDuration / DurationScale,
                lightActiveDuration / DurationScale,
                lightDamage,
                lightColor
            );
        }

        protected override void OnMoonCancel()
        {
            contactCaster?.DisableCasting();
            if (Boss == null) return;
            Boss.transform.DOKill();
            if (hasOrigin)
            {
                Boss.transform.position = originPosition;
                Boss.transform.localScale = originScale;
            }
            hasOrigin = false;
        }
    }
}
