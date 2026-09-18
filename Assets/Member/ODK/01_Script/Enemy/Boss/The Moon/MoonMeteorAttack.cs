using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    public class MoonMeteorAttack : MoonSkill
    {
        [Header("Meteor")]
        [SerializeField] private MoonBullet meteorPrefab;
        [SerializeField] private float patternDuration = 8f;
        [SerializeField] private float initialSpawnInterval = 1.4f;
        [SerializeField] private float finalSpawnInterval = 0.35f;
        [SerializeField] private float warningDuration = 0.75f;
        [SerializeField] private float phaseOneSpeed = 9f;
        [SerializeField] private float phaseTwoSpeed = 14f;
        [SerializeField] private float spawnMargin = 3f;
        [SerializeField] private float meteorAcceleration = 10f;
        [SerializeField] private float maximumMeteorSpeed = 24f;
        [SerializeField] private float meteorTurnSpeed = 6f;
        [SerializeField] private float playerDamage = 35f;
        [SerializeField] private float bossDamage = 100f;
        [SerializeField] private float meteorLifeTime = 10f;

        [Header("Side Pull Setup")]
        [SerializeField, Range(0.1f, 1f)] private float sidePositionRate = 0.72f;
        [SerializeField, Range(0f, 89f)] private float oppositeConeHalfAngle = 45f;
        [SerializeField] private float sideMoveDuration = 0.65f;
        [SerializeField] private float returnDuration = 0.5f;
        [SerializeField] private float movementLeanAngle = 12f;

        [Header("Global Shockwave")]
        [SerializeField] private DamageCaster shockwaveCaster;
        [SerializeField] private float shockwaveDamage = 15f;

        private readonly List<MoonBullet> activeMeteors = new List<MoonBullet>();
        private readonly List<OriginState> originStates = new List<OriginState>();

        public override bool CanUseSkill(GameObject target = null)
        {
            return Boss != null && !Boss.IsDead;
        }

        protected override void OnMoonInitialize()
        {
            if (shockwaveCaster != null) return;

            Transform existing = transform.Find("Meteor Shockwave Caster");
            if (existing != null) shockwaveCaster = existing.GetComponent<DamageCaster>();
            if (shockwaveCaster != null) return;

            GameObject casterObject = new GameObject("Meteor Shockwave Caster");
            casterObject.transform.SetParent(transform, false);
            shockwaveCaster = casterObject.AddComponent<DamageCaster>();
        }

        protected override IEnumerator ExecuteMoon(GameObject target)
        {
            CacheAndMoveOrigins();
            Boss.AttackReady(Boss.transform.position);
            yield return new WaitForSeconds(sideMoveDuration / DurationScale);

            float elapsed = 0f;
            while (elapsed < patternDuration)
            {
                float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, patternDuration));
                float interval = Mathf.Lerp(initialSpawnInterval, finalSpawnInterval, progress);

                foreach (Transform origin in Boss.GetPatternOrigins())
                    SpawnMeteor(origin);

                yield return new WaitForSeconds(interval / DurationScale);
                elapsed += interval;
            }

            CleanupMeteors();
            ReturnOrigins();
            yield return new WaitForSeconds(returnDuration / DurationScale);
        }

        private void CacheAndMoveOrigins()
        {
            originStates.Clear();
            float selectedSide = Random.value < 0.5f ? -1f : 1f;
            int index = 0;

            foreach (Transform origin in Boss.GetPatternOrigins())
            {
                if (origin == null) continue;

                OriginState state = new OriginState(origin, origin.position, origin.rotation);
                originStates.Add(state);

                float side = index == 0 ? selectedSide : -selectedSide;
                Vector3 destination = origin.position;
                destination.x = Boss.ArenaCenter.x +
                    Boss.ArenaHalfWidth * sidePositionRate * side;

                origin.DOKill();
                origin.DOMove(destination, sideMoveDuration / DurationScale)
                    .SetEase(Ease.OutBack);
                origin.DORotateQuaternion(
                    Quaternion.Euler(0f, 0f, -side * movementLeanAngle),
                    sideMoveDuration / DurationScale
                ).SetEase(Ease.OutCubic);
                index++;
            }
        }

        private void SpawnMeteor(Transform impactTarget)
        {
            if (impactTarget == null) return;

            Vector3 spawnPosition = GetConeSpawnPosition(impactTarget);
            MoonBullet meteor = meteorPrefab != null
                ? Instantiate(meteorPrefab, spawnPosition, Quaternion.identity)
                : CreateFallbackMeteor(spawnPosition);

            meteor.name = "Moon Meteor";
            meteor.OnBossImpact += HandleBossImpact;
            meteor.Launch(
                impactTarget,
                warningDuration / DurationScale,
                Boss.IsPhaseTwo ? phaseTwoSpeed : phaseOneSpeed,
                playerDamage,
                bossDamage,
                meteorLifeTime,
                0.6f,
                meteorAcceleration,
                maximumMeteorSpeed,
                meteorTurnSpeed
            );
            activeMeteors.Add(meteor);
        }

        private Vector3 GetConeSpawnPosition(Transform impactTarget)
        {
            Vector3 targetPosition = impactTarget.position;
            bool moonIsOnRight = targetPosition.x >= Boss.ArenaCenter.x;
            float oppositeDirectionAngle = moonIsOnRight ? 180f : 0f;
            float angle = oppositeDirectionAngle +
                Random.Range(-oppositeConeHalfAngle, oppositeConeHalfAngle);
            float distance = Mathf.Max(
                Boss.ArenaHalfWidth * 2f + spawnMargin,
                Boss.ArenaHalfHeight * 2f + spawnMargin
            );

            float radians = angle * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians));
            Vector3 spawnPosition = targetPosition + direction * distance;
            spawnPosition.z = Boss.ArenaCenter.z;
            return spawnPosition;
        }

        private MoonBullet CreateFallbackMeteor(Vector3 position)
        {
            GameObject meteorObject = new GameObject("Moon Meteor");
            meteorObject.transform.position = position;

            Rigidbody2D body = meteorObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            CircleCollider2D collider = meteorObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.45f;


            return meteorObject.AddComponent<MoonBullet>();
        }

        private void HandleBossImpact(Vector3 position)
        {
            float arenaRadius = Mathf.Sqrt(
                Boss.ArenaHalfWidth * Boss.ArenaHalfWidth +
                Boss.ArenaHalfHeight * Boss.ArenaHalfHeight
            );
            if (shockwaveCaster != null)
            {
                shockwaveCaster.ConfigureCircle(arenaRadius, Boss.PlayerLayer);
                shockwaveCaster.SetWorldPosition(position);
                shockwaveCaster.Cast(new DamageData(shockwaveDamage, DamageType.Special));
            }

            Boss.PlayShockwave(position, arenaRadius, new Color(0.45f, 0.75f, 1f, 0.75f));
            Boss.AttackImpact(position);
        }

        private void ReturnOrigins()
        {
            foreach (OriginState state in originStates)
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

        private void CleanupMeteors()
        {
            for (int i = activeMeteors.Count - 1; i >= 0; i--)
            {
                MoonBullet meteor = activeMeteors[i];
                if (meteor == null) continue;
                meteor.OnBossImpact -= HandleBossImpact;
                meteor.ForceRelease();
            }
            activeMeteors.Clear();
        }

        protected override void OnCancel()
        {
            CleanupMeteors();
            foreach (OriginState state in originStates)
            {
                if (state.Transform == null) continue;
                state.Transform.DOKill();
                state.Transform.SetPositionAndRotation(state.StartPosition, state.StartRotation);
            }
            originStates.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            MoonBoss moonBoss = Boss != null ? Boss : GetComponent<MoonBoss>();
            if (moonBoss == null) return;

            Vector3 center = moonBoss.ArenaCenter;
            float sideDistance = moonBoss.ArenaHalfWidth * sidePositionRate;
            float spawnDistance = Mathf.Max(
                moonBoss.ArenaHalfWidth * 2f + spawnMargin,
                moonBoss.ArenaHalfHeight * 2f + spawnMargin
            );

            DrawPullCone(center + Vector3.right * sideDistance, 180f, spawnDistance);
            DrawPullCone(center + Vector3.left * sideDistance, 0f, spawnDistance);
        }

        private void DrawPullCone(Vector3 moonPosition, float centerAngle, float distance)
        {
            Gizmos.color = new Color(0.45f, 0.75f, 1f, 0.65f);
            Gizmos.DrawWireSphere(moonPosition, 0.35f);
            Gizmos.DrawLine(
                moonPosition,
                moonPosition + Direction(centerAngle - oppositeConeHalfAngle) * distance
            );
            Gizmos.DrawLine(
                moonPosition,
                moonPosition + Direction(centerAngle + oppositeConeHalfAngle) * distance
            );
        }

        private static Vector3 Direction(float angle)
        {
            float radians = angle * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        private readonly struct OriginState
        {
            public readonly Transform Transform;
            public readonly Vector3 StartPosition;
            public readonly Quaternion StartRotation;

            public OriginState(Transform transform, Vector3 startPosition, Quaternion startRotation)
            {
                Transform = transform;
                StartPosition = startPosition;
                StartRotation = startRotation;
            }
        }
    }
}
