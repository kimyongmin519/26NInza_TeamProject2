using System.Collections;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    public class MoonTraverseShowerAttack : MoonSkill
    {
        [Header("Traverse")]
        [SerializeField] private int horizontalJumpCount = 6;
        [SerializeField] private float edgePadding = 1.2f;
        [SerializeField] private float smallJumpHeight = 3.4f;
        [SerializeField] private float smallJumpDuration = 0.34f;

        [Header("Final Jump")]
        [SerializeField] private float finalJumpHeight = 4.5f;
        [SerializeField] private float finalJumpDuration = 0.9f;
        [SerializeField] private float fragmentInterval = 0.1f;
        [SerializeField] private Vector2Int fragmentsPerBurst = new Vector2Int(2, 4);
        [SerializeField] private Vector2 fragmentScaleRange = new Vector2(0.35f, 1.25f);
        [SerializeField] private float fragmentSpawnSpread = 1.5f;
        [SerializeField] private float fragmentDamage = DamageCaster.BossPlayerDamage;
        [SerializeField] private float fragmentSpread = 4f;
        [SerializeField] private float landingDamage = DamageCaster.BossPlayerDamage;
        [SerializeField] private float landingRadius = 2.2f;
        [SerializeField] private float returnDuration = 0.45f;

        private DamageCaster landingCaster;
        private Vector3 originPosition;
        private bool hasOrigin;
        protected override bool UsesAmbientFloating => false;

        public override bool CanUseSkill(GameObject target = null)
        {
            return Boss != null && !Boss.IsDead;
        }

        protected override void OnMoonInitialize()
        {
            landingCaster = CreateCaster("Traverse Landing Caster");
        }

        protected override IEnumerator ExecuteMoon(GameObject target)
        {
            originPosition = Boss.transform.position;
            hasOrigin = true;
            float leftX = Boss.ArenaCenter.x - Boss.ArenaHalfWidth + edgePadding;
            float rightX = Boss.ArenaCenter.x + Boss.ArenaHalfWidth - edgePadding;
            int count = Mathf.Max(2, horizontalJumpCount);

            for (int i = 0; i < count; i++)
            {
                float rate = i / (float)(count - 1);
                Vector3 groundPoint = Boss.GetGroundPoint(Mathf.Lerp(leftX, rightX, rate));
                groundPoint.z = Boss.transform.position.z;
                Vector3 landing = Boss.GetImpactVisualPosition(
                    Boss.transform,
                    groundPoint,
                    Vector2.zero
                );
                Boss.PlayJumpFeedback(Boss.transform.position);
                yield return MoonJumpSlamAttack.MoveArc(
                    Boss.transform,
                    Boss.transform.position,
                    landing,
                    smallJumpHeight,
                    smallJumpDuration * Boss.JumpTimeScale / DurationScale
                );
                Boss.ShakeImpact(false);
                Boss.PlayLandingFeedback(groundPoint, false);
            }

            Vector3 finalGroundPoint = Boss.GetGroundPoint(leftX);
            finalGroundPoint.z = Boss.transform.position.z;
            Vector3 finalLanding = Boss.GetImpactVisualPosition(
                Boss.transform,
                finalGroundPoint,
                Vector2.zero
            );
            Boss.AttackReady(finalGroundPoint);
            Boss.PlayJumpFeedback(Boss.transform.position);
            yield return FinalJumpWithFragments(finalLanding);

            landingCaster.ConfigureCircle(landingRadius, Boss.PlayerLayer);
            landingCaster.SetWorldPosition(finalGroundPoint);
            landingCaster.Cast(new DamageData(landingDamage, DamageType.Melee));
            Boss.ShakeImpact(true);
            Boss.PlayLandingFeedback(finalGroundPoint, true);
            Boss.AttackImpact(finalGroundPoint);

            Boss.transform.DOKill();
            yield return Boss.transform.DOMove(originPosition, returnDuration / DurationScale)
                .SetEase(Ease.InOutSine)
                .WaitForCompletion();
            hasOrigin = false;
        }

        private IEnumerator FinalJumpWithFragments(Vector3 landing)
        {
            Vector3 start = Boss.transform.position;
            float progress = 0f;
            Tween jump = DOTween.To(() => progress, value =>
                {
                    progress = value;
                    Vector3 point = Vector3.Lerp(start, landing, value);
                    point.y += 4f * finalJumpHeight * value * (1f - value);
                    Boss.transform.position = point;
                }, 1f, finalJumpDuration * Boss.JumpTimeScale / DurationScale)
                .SetEase(Ease.Linear)
                .SetTarget(Boss.transform);

            float fragmentTimer = 0f;
            while (jump.IsActive() && jump.IsPlaying())
            {
                fragmentTimer -= Time.deltaTime * DurationScale;
                if (fragmentTimer <= 0f)
                {
                    SpawnFragmentBurst();
                    fragmentTimer = fragmentInterval * Boss.JumpTimeScale;
                }
                yield return null;
            }
            Boss.transform.position = landing;
        }

        private void SpawnFragmentBurst()
        {
            int minimum = Mathf.Max(1, fragmentsPerBurst.x);
            int maximum = Mathf.Max(minimum, fragmentsPerBurst.y);
            int count = Random.Range(minimum, maximum + 1);
            float minimumScale = Mathf.Min(fragmentScaleRange.x, fragmentScaleRange.y);
            float maximumScale = Mathf.Max(fragmentScaleRange.x, fragmentScaleRange.y);

            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPosition = Boss.transform.position + Vector3.right *
                    Random.Range(-fragmentSpawnSpread, fragmentSpawnSpread);
                Vector2 velocity = new Vector2(
                    Random.Range(-fragmentSpread, fragmentSpread),
                    Random.Range(-3.5f, -0.6f)
                );
                MoonHazardProjectile fragment = Boss.SpawnHazard(
                    spawnPosition,
                    velocity,
                    MoonHazardProjectile.MoveMode.Falling,
                    null,
                    fragmentDamage,
                    5f,
                    null,
                    new Color(0.75f, 0.82f, 1f, 0.9f)
                );
                if (fragment != null)
                    fragment.transform.localScale = Vector3.one * Random.Range(
                        Mathf.Max(0.1f, minimumScale),
                        Mathf.Max(0.1f, maximumScale)
                    );
            }

            Boss.PlayFragmentFeedback(Boss.transform.position);
        }

        protected override void OnMoonCancel()
        {
            if (Boss == null) return;
            Boss.transform.DOKill();
            if (hasOrigin) Boss.transform.position = originPosition;
            hasOrigin = false;
        }
    }
}
