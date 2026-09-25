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
        [SerializeField] private float dashPeriod = 1f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float dashSpeed = 35f;
        [SerializeField] private float homingStrength = 11f;
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
            yield return Boss.transform.DOScale(originScale * shrinkScale, shrinkDuration / DurationScale)
                .SetEase(Ease.InBack)
                .WaitForCompletion();

            int count = Mathf.Max(1, dashCount);
            for (int i = 0; i < count && !Boss.IsDead; i++)
            {
                float readyTime = Mathf.Max(0f, dashPeriod - dashDuration);
                Boss.AttackReady(Boss.Target != null ? Boss.Target.position : Boss.transform.position);
                yield return new WaitForSeconds(readyTime / DurationScale);

                if (Boss.IsPhaseTwo && lightEveryDash > 0 && (i + 1) % lightEveryDash == 0)
                    FireLight();

                yield return Dash();
            }

            contactCaster.DisableCasting();
            Boss.transform.DOKill();
            Sequence restore = DOTween.Sequence();
            restore.Join(Boss.transform.DOScale(originScale, 0.35f / DurationScale).SetEase(Ease.OutBack));
            restore.Join(Boss.transform.DOMove(originPosition, 0.45f / DurationScale).SetEase(Ease.InOutSine));
            yield return restore.WaitForCompletion();
            hasOrigin = false;
        }

        private IEnumerator Dash()
        {
            Vector2 direction = Boss.Target != null
                ? ((Vector2)Boss.Target.position - (Vector2)Boss.transform.position).normalized
                : Vector2.right;
            float actualDuration = dashDuration / DurationScale;
            contactCaster.transform.position = Boss.transform.position;
            contactCaster.ConfigureCircle(contactRadius, Boss.PlayerLayer);
            contactCaster.EnableCasting(
                new DamageData(contactDamage, DamageType.Melee),
                actualDuration
            );

            float elapsed = 0f;
            while (elapsed < actualDuration && !Boss.IsDead)
            {
                float delta = Time.deltaTime;
                if (Boss.Target != null)
                {
                    Vector2 desired = ((Vector2)Boss.Target.position - (Vector2)Boss.transform.position).normalized;
                    float rate = 1f - Mathf.Exp(-homingStrength * delta);
                    direction = Vector2.Lerp(direction, desired, rate).normalized;
                }

                Vector3 next = Boss.transform.position + (Vector3)direction * dashSpeed * delta;
                if (Boss.Arena != null) next = Boss.Arena.Clamp(next, contactRadius);
                next.z = Boss.transform.position.z;
                Boss.transform.position = next;
                contactCaster.transform.position = next;
                elapsed += delta;
                yield return null;
            }

            contactCaster.DisableCasting();
            Boss.AttackImpact(Boss.transform.position);
        }

        private void FireLight()
        {
            if (Boss.Target == null) return;
            Vector2 direction = Boss.Target.position - Boss.transform.position;
            float length = Mathf.Sqrt(
                Boss.ArenaHalfWidth * Boss.ArenaHalfWidth +
                Boss.ArenaHalfHeight * Boss.ArenaHalfHeight
            ) * 2.5f;
            MoonLaserShot.Spawn(
                Boss.transform.position,
                direction,
                length,
                lightWidth,
                lightWarningDuration / DurationScale,
                lightActiveDuration / DurationScale,
                lightDamage,
                Boss.PlayerLayer,
                lightColor
            );
        }

        protected override void OnCancel()
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
