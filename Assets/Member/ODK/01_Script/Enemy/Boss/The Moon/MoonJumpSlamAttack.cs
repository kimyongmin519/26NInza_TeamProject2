using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    public class MoonJumpSlamAttack : MoonSkill
    {
        [Header("Jump Slam")]
        [SerializeField] private int slamCount = 9;
        [SerializeField] private float warningDuration = 0.38f;
        [SerializeField] private float jumpDuration = 0.48f;
        [SerializeField] private float jumpHeight = 2.6f;
        [SerializeField] private float landingDamage = 34f;
        [SerializeField] private float landingRadius = 1.8f;
        [SerializeField, Range(0f, 1f)] private float playerAreaChance = 0.65f;
        [SerializeField] private float playerPositionSpread = 3.2f;

        [Header("Phase Two Strong Slam")]
        [SerializeField] private float strongJumpHeightMultiplier = 1.4f;
        [SerializeField] private float strongDamageMultiplier = 1.7f;
        [SerializeField] private float sideProjectileSpeed = 13f;
        [SerializeField] private float sideProjectileDamage = 26f;
        [SerializeField] private float sideProjectileHeight = 0.75f;

        [Header("Rock")]
        [SerializeField] private Vector2 rockLaunchVelocity = new Vector2(2.5f, 11.5f);
        [SerializeField, Min(1)] private int normalRockCount = 3;
        [SerializeField, Min(1)] private int strongRockCount = 6;
        [SerializeField] private float rockHorizontalSpread = 1.25f;

        private DamageCaster landingCaster;
        private MoonTelegraphLine trajectoryLine;
        private Vector3 originPosition;
        private bool hasOrigin;
        protected override bool UsesAmbientFloating => false;

        public override bool CanUseSkill(GameObject target = null)
        {
            return Boss != null && Boss.Target != null && !Boss.IsDead;
        }

        protected override void OnMoonInitialize()
        {
            landingCaster = CreateCaster("Jump Slam Caster");
            trajectoryLine = Boss.SpawnTelegraph(transform);
        }

        protected override IEnumerator ExecuteMoon(GameObject target)
        {
            originPosition = Boss.transform.position;
            hasOrigin = true;

            int count = Mathf.Max(1, slamCount);
            for (int i = 0; i < count && !Boss.IsDead; i++)
            {
                bool strong = Boss.IsPhaseTwo && (i + 1) % 3 == 0;
                float targetX = ChooseLandingX();
                targetX = Mathf.Clamp(
                    targetX,
                    Boss.ArenaCenter.x - Boss.ArenaHalfWidth,
                    Boss.ArenaCenter.x + Boss.ArenaHalfWidth
                );
                Vector3 groundPoint = Boss.GetGroundPoint(targetX);
                groundPoint.z = Boss.transform.position.z;
                Vector3 landingPoint = Boss.GetImpactVisualPosition(
                    Boss.transform,
                    groundPoint,
                    Vector2.zero
                );
                float height = jumpHeight * (strong ? strongJumpHeightMultiplier : 1f);
                List<Vector3> path = BuildArc(Boss.transform.position, landingPoint, height);

                float prepareDuration = i == 0 ? warningDuration / DurationScale : 0f;
                float lineDuration = prepareDuration > 0f
                    ? prepareDuration * 0.75f
                    : jumpDuration * 0.75f / DurationScale;
                trajectoryLine?.Show(path, lineDuration);
                Boss.AttackReady(groundPoint);
                if (prepareDuration > 0f)
                    yield return new WaitForSeconds(prepareDuration);

                Boss.PlayJumpFeedback(Boss.transform.position);
                yield return MoveArc(Boss.transform, Boss.transform.position, landingPoint, height, jumpDuration / DurationScale);
                trajectoryLine?.Hide();
                Land(groundPoint, strong);
            }

            Boss.transform.DOKill();
            yield return Boss.transform.DOMove(originPosition, 0.45f / DurationScale)
                .SetEase(Ease.InOutSine)
                .WaitForCompletion();
            hasOrigin = false;
        }

        private float ChooseLandingX()
        {
            float minimum = Boss.ArenaCenter.x - Boss.ArenaHalfWidth;
            float maximum = Boss.ArenaCenter.x + Boss.ArenaHalfWidth;
            bool usePlayerArea = Boss.Target != null && Random.value < playerAreaChance;
            float targetX = usePlayerArea
                ? Boss.Target.position.x + Random.Range(-playerPositionSpread, playerPositionSpread)
                : Random.Range(minimum, maximum);
            return Mathf.Clamp(targetX, minimum, maximum);
        }

        private void Land(Vector3 point, bool strong)
        {
            float radius = landingRadius * (strong ? 1.25f : 1f);
            float damage = landingDamage * (strong ? strongDamageMultiplier : 1f);
            landingCaster.ConfigureCircle(radius, Boss.PlayerLayer);
            landingCaster.SetWorldPosition(point);
            landingCaster.Cast(new DamageData(damage, DamageType.Melee));

            int rockCount = strong ? strongRockCount : normalRockCount;
            for (int i = 0; i < rockCount; i++)
            {
                float spread = rockCount == 1
                    ? 0f
                    : Mathf.Lerp(-rockHorizontalSpread, rockHorizontalSpread, i / (float)(rockCount - 1));
                Vector2 velocity = new Vector2(
                    rockLaunchVelocity.x * spread + Random.Range(-0.8f, 0.8f),
                    rockLaunchVelocity.y * (strong ? 1.25f : 1f) * Random.Range(0.95f, 1.3f)
                );
                Boss.SpawnRock(point + Vector3.up * 0.55f, velocity, strong);
            }

            if (strong) SpawnSideProjectiles(point.y + sideProjectileHeight);
            Boss.ShakeImpact(strong);
            Boss.PlayLandingFeedback(point, strong);
            Boss.AttackImpact(point);
        }

        private void SpawnSideProjectiles(float height)
        {
            Vector3 center = Boss.ArenaCenter;
            Vector3 left = new Vector3(center.x - Boss.ArenaHalfWidth, height, center.z);
            Vector3 right = new Vector3(center.x + Boss.ArenaHalfWidth, height, center.z);
            Boss.SpawnHazard(
                left, Vector2.right * sideProjectileSpeed,
                MoonHazardProjectile.MoveMode.Linear, null,
                sideProjectileDamage, 4f
            );
            Boss.SpawnHazard(
                right, Vector2.left * sideProjectileSpeed,
                MoonHazardProjectile.MoveMode.Linear, null,
                sideProjectileDamage, 4f
            );
        }

        private static List<Vector3> BuildArc(Vector3 start, Vector3 end, float height)
        {
            const int pointCount = 18;
            List<Vector3> points = new List<Vector3>(pointCount);
            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);
                Vector3 point = Vector3.Lerp(start, end, t);
                point.y += 4f * height * t * (1f - t);
                points.Add(point);
            }
            return points;
        }

        public static IEnumerator MoveArc(
            Transform moving,
            Vector3 start,
            Vector3 end,
            float height,
            float duration)
        {
            float progress = 0f;
            Tween tween = DOTween.To(() => progress, value =>
                {
                    progress = value;
                    Vector3 point = Vector3.Lerp(start, end, value);
                    point.y += 4f * height * value * (1f - value);
                    moving.position = point;
                }, 1f, Mathf.Max(0.02f, duration))
                .SetEase(Ease.Linear)
                .SetTarget(moving);
            yield return tween.WaitForCompletion();
        }

        protected override void OnMoonCancel()
        {
            trajectoryLine?.Hide();
            if (Boss == null) return;
            Boss.transform.DOKill();
            if (hasOrigin) Boss.transform.position = originPosition;
            hasOrigin = false;
        }
    }
}
