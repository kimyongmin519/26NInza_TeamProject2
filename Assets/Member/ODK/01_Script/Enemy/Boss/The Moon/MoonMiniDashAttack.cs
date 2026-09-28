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
        [SerializeField, Min(1)] private int dashCountCap = 8;
        [SerializeField] private float contactRadius = 0.85f;
        [SerializeField] private float contactDamage = DamageCaster.BossPlayerDamage;
        [SerializeField] private MoonDashMotion motion = new MoonDashMotion();

        [Header("Dash Feel")]
        [SerializeField, Range(0f, 1f)] private float wallBounceKeep = 0.55f;

        [Header("Phase Two Light")]
        [SerializeField] private int lightEveryDash = 2;
        [SerializeField] private float lightWarningDuration = 0.28f;
        [SerializeField] private float lightActiveDuration = 0.16f;
        [SerializeField] private float lightWidth = 0.3f;
        [SerializeField] private float lightDamage = DamageCaster.BossPlayerDamage;
        [SerializeField] private Color lightColor = new Color(0.65f, 0.82f, 1f, 1f);

        private DamageCaster contactCaster;
        private Vector3 originPosition;
        private Vector3 originScale;
        private Vector3 shrunkScale;
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
            shrunkScale = originScale * shrinkScale;
            hasOrigin = true;
            Boss.transform.DOKill();
            Boss.PlayShrinkFeedback(Boss.transform.position);
            yield return Boss.transform.DOScale(shrunkScale, shrinkDuration / DurationScale)
                .SetEase(Ease.InQuad)
                .WaitForCompletion();

            motion.Begin(Vector2.zero, DurationScale, Boss.DashPowerScale, Boss.DashIntervalScale);
            contactCaster.transform.position = Boss.transform.position;
            contactCaster.ConfigureCircle(contactRadius, Boss.PlayerLayer);

            int total = Mathf.Clamp(dashCount, 1, Mathf.Max(1, dashCountCap));
            int dashed = 0;
            float tail = motion.DashInterval / DurationScale;
            float tailTimer = 0f;
            while (!Boss.IsDead && (dashed < total || tailTimer < tail))
            {
                float delta = Time.deltaTime;
                Vector2 position = Boss.transform.position;
                Vector2 targetPosition = Boss.Target != null ? (Vector2)Boss.Target.position : position;

                bool dashNow = motion.Tick(position, targetPosition, delta);
                if (dashNow && dashed < total)
                {
                    dashed++;
                    OnDash(dashed);
                }
                else if (dashNow)
                {
                    motion.Stop();
                }

                if (dashed >= total) tailTimer += delta;

                Vector3 next = Boss.transform.position + (Vector3)(motion.Velocity * delta);
                Vector3 clamped = ClampToArena(next);
                Vector2 wallNormal = new Vector2(
                    Mathf.Approximately(clamped.x, next.x) ? 0f : Mathf.Sign(clamped.x - next.x),
                    Mathf.Approximately(clamped.y, next.y) ? 0f : Mathf.Sign(clamped.y - next.y));
                if (wallNormal != Vector2.zero)
                {
                    motion.Bounce(wallNormal, wallBounceKeep);
                    Boss.ShakeCamera(0.35f);
                }
                clamped.z = Boss.transform.position.z;
                Boss.transform.position = clamped;
                contactCaster.transform.position = clamped;
                yield return null;
            }

            contactCaster.DisableCasting();
            Boss.transform.DOKill();
            Sequence restore = DOTween.Sequence().SetTarget(Boss.transform);
            restore.Join(Boss.transform.DOScale(originScale, 0.35f / DurationScale).SetEase(Ease.OutQuad));
            restore.Join(Boss.transform.DOMove(originPosition, 0.45f / DurationScale).SetEase(Ease.InOutSine));
            yield return restore.WaitForCompletion();
            hasOrigin = false;
        }

        private void OnDash(int index)
        {
            contactCaster.DisableCasting();
            contactCaster.EnableCasting(
                new DamageData(contactDamage, DamageType.Melee),
                motion.DashInterval / DurationScale);
            Boss.PlayDashFeedback(Boss.transform.position);
            Boss.AttackImpact(Boss.transform.position);
            Boss.ShakeCamera(0.42f);
            if (Boss.IsPhaseTwo && lightEveryDash > 0 && index % lightEveryDash == 0)
                FireLight();
        }

        private Vector3 ClampToArena(Vector3 position)
        {
            return Boss.Arena != null ? Boss.Arena.Clamp(position, contactRadius) : position;
        }

        private void FireLight()
        {
            if (Boss.Target == null) return;
            Vector2 direction = Boss.GetLaserAimDirection(Boss.transform.position);
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
            motion.Stop();
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
