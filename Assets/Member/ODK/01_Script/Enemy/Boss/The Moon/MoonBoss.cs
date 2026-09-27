using System.Collections;
using System.Collections.Generic;
using GGMLib.ObjectPool.Runtime;
using DG.Tweening;
using Member.ODK._01_Script;
using Member.ODK.Scripts.Enemys.Bosses;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    public class MoonBoss : PhasedBossController, IDamageable
    {
        [Header("Collision")]
        [SerializeField] private LayerMask playerLayer = 1 << 6;
        [SerializeField] private LayerMask hazardGroundLayer = 1 << 3;

        [Header("Hitbox")]
        [SerializeField] private float hitboxRadius;
        [SerializeField] private Vector2 hitboxOffset;
        [SerializeField, Range(0.3f, 1.2f)] private float autoRadiusRatio = 0.8f;

        [Header("Rock")]
        [SerializeField] private MoonRock rockPrefab;
        [SerializeField] private MoonHazardProjectile hazardProjectilePrefab;
        [SerializeField] private MoonLaserShot laserShotPrefab;
        [SerializeField] private MoonTelegraphLine telegraphLinePrefab;
        [SerializeField] private float rockBossDamage = 1f;
        [SerializeField] private float rockPlayerDamage = 28f;
        [SerializeField] private float rockExplosionRadius = 1.4f;
        [SerializeField] private float spawnedRockLifeTime = 12f;
        [SerializeField] private Vector2 rockScaleRange = new Vector2(0.55f, 1.65f);

        [Header("Balance")]
        [SerializeField, Min(0.5f)] private float jumpTimeScale = 1.45f;
        [SerializeField, Min(1f)] private float laserWarningScale = 1.35f;
        [SerializeField, Min(0f)] private float laserWarningBonus = 0.08f;
        [SerializeField, Min(0f)] private float laserAimSpread = 11f;
        [SerializeField, Min(0f)] private float laserAimLag = 0.28f;
        [SerializeField, Min(0.05f)] private float rockGravityScale = 1.5f;
        [SerializeField, Min(0.5f)] private float rockLaunchBoost = 1.15f;
        [SerializeField, Min(0.05f)] private float fallingGravityScale = 1.2f;
        [SerializeField, Range(0.2f, 1f)] private float dashPowerScale = 0.75f;
        [SerializeField, Min(0.5f)] private float dashIntervalScale = 1.3f;
        [SerializeField, Range(0.1f, 1f)] private float dashDamageScale = 0.75f;

        [Header("Impact Effect")]
        [SerializeField] private PoolItemSO impactEffectItem;
        [SerializeField] private string impactEffectName = "ProjectileImpact";
        [SerializeField] private Color rockImpactColor = new Color(0.85f, 0.88f, 1f, 1f);
        [SerializeField] private Color hitImpactColor = new Color(0.7f, 0.85f, 1f, 1f);
        [SerializeField, Min(0f)] private float hitEffectInterval = 0.05f;

        [Header("Animation")]
        [SerializeField] private Animator moonAnimator;
        [SerializeField] private string phaseTwoAnimationState;
        [SerializeField] private float phaseTransitionDuration = 1f;

        [Header("Ambient Floating")]
        [SerializeField] private Transform floatingVisual;
        [SerializeField] private Vector2 floatingDistance = new Vector2(0.16f, 0.32f);
        [SerializeField] private float floatingDuration = 1.6f;

        [Header("Camera Impulse")]
        [SerializeField] private CinemachineImpulseSource impulseSource;
        [SerializeField] private float normalImpactImpulse = 1.2f;
        [SerializeField] private float strongImpactImpulse = 2.4f;

        [Header("Feedback")]
        [SerializeField] private MoonBossFeedback feedback;
        [SerializeField] private BossPositionEvent onDamaged;
        [SerializeField] private UnityEvent onPhaseTwoVisual;

        public LayerMask PlayerLayer => playerLayer;
        public LayerMask GroundLayer => hazardGroundLayer;
        public float JumpTimeScale => jumpTimeScale;
        public float DashPowerScale => dashPowerScale;
        public float DashIntervalScale => dashIntervalScale;
        public float DashDamageScale => dashDamageScale;
        public float FallingGravityScale => fallingGravityScale;
        protected override float PhaseTransitionDelay => phaseTransitionDuration;

        private int lastAttackIndex = -1;
        private Vector3 floatingOrigin;
        private Sequence floatingTween;
        private readonly Queue<Vector4> targetHistory = new Queue<Vector4>();
        private float lastHitEffectTime = -10f;

        private enum AttackIndex
        {
            JumpSlam = 0,
            OrbitBeam = 1,
            TraverseShower = 2,
            MiniDash = 3
        }

        private const int AttackCount = (int)AttackIndex.MiniDash + 1;

        protected override void Awake()
        {
            base.Awake();
            if (moonAnimator == null) moonAnimator = GetComponentInChildren<Animator>();
            if (floatingVisual == null && moonAnimator != null) floatingVisual = moonAnimator.transform;
            if (feedback == null) feedback = GetComponent<MoonBossFeedback>();
            if (feedback == null) feedback = gameObject.AddComponent<MoonBossFeedback>();
            if (impulseSource == null) impulseSource = GetComponent<CinemachineImpulseSource>();
            if (impulseSource == null) impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
            ConfigureHitbox();
            StartAmbientFloating();
        }

        private void LateUpdate()
        {
            if (Target == null) return;
            Vector3 position = Target.position;
            targetHistory.Enqueue(new Vector4(position.x, position.y, position.z, Time.time));
            float keep = laserAimLag + 0.5f;
            while (targetHistory.Count > 0 && Time.time - targetHistory.Peek().w > keep)
                targetHistory.Dequeue();
        }

        public Vector3 GetLaggedTargetPosition()
        {
            if (Target == null) return transform.position;
            float wanted = Time.time - laserAimLag;
            foreach (Vector4 sample in targetHistory)
            {
                if (sample.w >= wanted)
                    return new Vector3(sample.x, sample.y, sample.z);
            }
            return Target.position;
        }

        public Vector2 GetLaserAimDirection(Vector3 origin, float extraSpread = 0f)
        {
            Vector2 direction = GetLaggedTargetPosition() - origin;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector2.down;
            float spread = laserAimSpread + Mathf.Max(0f, extraSpread);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + Random.Range(-spread, spread);
            return new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        }

        public void PlayImpactEffect(Vector3 position, Vector2 normal, Color tint)
        {
            PoolItemSO item = impactEffectItem != null ? impactEffectItem : ODKPool.FindItem(impactEffectName);
            if (item == null) return;
            Vector2 facing = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector2.up;
            float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            ODKPool.PlayEffect(item, position, Quaternion.Euler(0f, 0f, angle), tint);
        }

        public void PlayRockImpactEffect(Vector3 position, Vector2 normal)
        {
            PlayImpactEffect(position, normal, rockImpactColor);
        }

        private void PlayHitEffect()
        {
            if (Time.time - lastHitEffectTime < hitEffectInterval) return;
            lastHitEffectTime = Time.time;
            CircleCollider2D hitbox = GetComponent<CircleCollider2D>();
            Vector3 center = hitbox != null ? (Vector3)(Vector2)hitbox.bounds.center : transform.position;
            float radius = hitbox != null ? Mathf.Max(hitbox.bounds.extents.x, hitbox.bounds.extents.y) : 1f;
            Vector2 toward = Target != null ? (Vector2)(Target.position - center) : Random.insideUnitCircle;
            if (toward.sqrMagnitude < 0.0001f) toward = Vector2.up;
            toward = toward.normalized;
            Vector2 jitter = Random.insideUnitCircle * radius * 0.2f;
            Vector3 point = center + (Vector3)(toward * radius * 0.7f + jitter);
            point.z = transform.position.z;
            PlayImpactEffect(point, toward, hitImpactColor);
        }

        private void ConfigureHitbox()
        {
            CircleCollider2D hitbox = GetComponent<CircleCollider2D>();
            if (hitbox == null) hitbox = gameObject.AddComponent<CircleCollider2D>();
            hitbox.enabled = true;
            hitbox.isTrigger = true;

            float radius = hitboxRadius;
            Vector2 offset = hitboxOffset;
            if (radius <= 0f)
            {
                SpriteRenderer body = floatingVisual != null
                    ? floatingVisual.GetComponentInChildren<SpriteRenderer>()
                    : GetComponentInChildren<SpriteRenderer>();
                if (body != null && body.sprite != null)
                {
                    Bounds bounds = body.bounds;
                    Vector3 lossy = transform.lossyScale;
                    float scale = Mathf.Max(0.0001f, Mathf.Max(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y)));
                    radius = Mathf.Max(bounds.extents.x, bounds.extents.y) * autoRadiusRatio / scale;
                    if (hitboxOffset == Vector2.zero)
                        offset = transform.InverseTransformPoint(bounds.center);
                }
                else
                {
                    radius = 1.2f;
                }
            }

            hitbox.radius = Mathf.Max(0.05f, radius);
            hitbox.offset = offset;
        }

        protected override IEnumerator PhaseOneLoop()
        {
            yield return PlayRandomAttack();
            yield return AttackWait();
        }

        protected override IEnumerator PhaseTwoLoop()
        {
            yield return PlayRandomAttack();
            yield return AttackWait();
        }

        private IEnumerator PlayRandomAttack()
        {
            int attackIndex;
            do
            {
                attackIndex = Random.Range(0, AttackCount);
            }
            while (attackIndex == lastAttackIndex);

            lastAttackIndex = attackIndex;
            yield return PlayAttack(attackIndex);
        }

        public void PlayAnimation(string stateName)
        {
            if (moonAnimator == null || string.IsNullOrWhiteSpace(stateName)) return;
            int stateHash = Animator.StringToHash(stateName);
            if (moonAnimator.HasState(0, stateHash))
                moonAnimator.CrossFade(stateHash, 0.08f, 0, 0f);
        }

        public void SetAmbientFloating(bool enabled)
        {
            if (floatingVisual == null) return;
            if (enabled)
            {
                floatingVisual.localPosition = floatingOrigin;
                floatingTween?.Restart();
                return;
            }

            floatingTween?.Pause();
            floatingVisual.localPosition = floatingOrigin;
        }

        private void StartAmbientFloating()
        {
            if (floatingVisual == null) return;
            floatingOrigin = floatingVisual.localPosition;
            float halfDuration = Mathf.Max(0.1f, floatingDuration * 0.5f);
            floatingTween?.Kill();
            floatingTween = DOTween.Sequence().SetTarget(floatingVisual);
            floatingTween.Append(floatingVisual.DOLocalMove(
                floatingOrigin + new Vector3(floatingDistance.x, floatingDistance.y, 0f),
                halfDuration
            ).SetEase(Ease.InOutSine));
            floatingTween.Append(floatingVisual.DOLocalMove(
                floatingOrigin + new Vector3(-floatingDistance.x, -floatingDistance.y * 0.55f, 0f),
                halfDuration
            ).SetEase(Ease.InOutSine));
            floatingTween.SetLoops(-1, LoopType.Yoyo);
        }

        public MoonRock SpawnRock(Vector3 position, Vector2 velocity, bool strong)
        {
            if (rockPrefab == null) return null;
            MoonRock rock = ODKPool.Spawn(rockPrefab, position, Quaternion.identity);
            if (rock == null) return null;
            if (velocity.y > 0f) velocity.y *= rockLaunchBoost;
            float minimumScale = Mathf.Min(rockScaleRange.x, rockScaleRange.y);
            float maximumScale = Mathf.Max(rockScaleRange.x, rockScaleRange.y);
            float scaleMultiplier = Random.Range(
                Mathf.Max(0.1f, minimumScale),
                Mathf.Max(0.1f, maximumScale)
            );
            rock.transform.localScale *= scaleMultiplier;
            rock.Initialize(
                this,
                velocity,
                rockBossDamage,
                rockPlayerDamage * (strong ? 1.25f : 1f),
                rockExplosionRadius * scaleMultiplier * (strong ? 1.3f : 1f),
                spawnedRockLifeTime,
                playerLayer,
                rockGravityScale
            );
            return rock;
        }

        public MoonHazardProjectile SpawnHazard(
            Vector3 position,
            Vector2 velocity,
            MoonHazardProjectile.MoveMode moveMode,
            Transform homingTarget,
            float damage,
            float lifeTime,
            Sprite sprite = null,
            Color? color = null)
        {
            if (hazardProjectilePrefab == null) return null;
            MoonHazardProjectile projectile = ODKPool.Spawn(
                hazardProjectilePrefab,
                position,
                Quaternion.identity
            );
            if (projectile == null) return null;
            projectile.Initialize(
                velocity,
                moveMode,
                homingTarget,
                damage,
                lifeTime,
                playerLayer,
                hazardGroundLayer,
                sprite,
                color,
                fallingGravityScale
            );
            return projectile;
        }

        public MoonLaserShot SpawnLaser(
            Vector3 origin,
            Vector2 direction,
            float length,
            float width,
            float warningDuration,
            float activeDuration,
            float damage,
            Color color)
        {
            if (laserShotPrefab == null) return null;
            MoonLaserShot shot = ODKPool.Spawn(laserShotPrefab, origin, Quaternion.identity);
            if (shot == null) return null;
            shot.Initialize(
                this,
                origin,
                direction,
                length,
                width,
                warningDuration * laserWarningScale + laserWarningBonus,
                activeDuration,
                damage,
                playerLayer,
                color
            );
            return shot;
        }

        public MoonTelegraphLine SpawnTelegraph(Transform parent)
        {
            if (telegraphLinePrefab == null) return null;
            return Instantiate(telegraphLinePrefab, parent);
        }

        public void ShakeImpact(bool strong)
        {
            ShakeCamera(strong ? strongImpactImpulse : normalImpactImpulse);
        }

        public void PlayJumpFeedback(Vector3 position) => feedback?.PlayJump(position);
        public void PlayLandingFeedback(Vector3 position, bool strong) => feedback?.PlayLanding(position, strong);
        public void PlayFragmentFeedback(Vector3 position) => feedback?.PlayFragment(position);
        public void PlayRockExplosionFeedback(Vector3 position) => feedback?.PlayRockExplosion(position);
        public void PlayCloneFeedback(Vector3 position) => feedback?.PlayCloneThrow(position);
        public void PlayLaserFeedback(Vector3 position) => feedback?.PlayLaserFire(position);
        public void PlayShrinkFeedback(Vector3 position) => feedback?.PlayShrink(position);
        public void PlayDashFeedback(Vector3 position) => feedback?.PlayDash(position);

        public void TakeDamage(DamageData damage)
        {
            if (IsDead) return;
            onDamaged?.Invoke(transform.position);
            PlayHitEffect();
            ApplyBossDamage(damage);
        }

        protected override void OnPhaseTwoEntered()
        {
            PlayAnimation(phaseTwoAnimationState);
            feedback?.PlayPhaseTwo(transform.position);
            onPhaseTwoVisual?.Invoke();
            ShakeCamera(0.9f);
        }

        protected override void OnBossDeath() => ShakeCamera(1.25f);

        protected override void OnDestroy()
        {
            floatingTween?.Kill();
            base.OnDestroy();
        }
    }
}
