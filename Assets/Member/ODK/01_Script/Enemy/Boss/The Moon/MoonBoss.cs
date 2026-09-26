using System.Collections;
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

        [Header("Rock")]
        [SerializeField] private MoonRock rockPrefab;
        [SerializeField] private MoonHazardProjectile hazardProjectilePrefab;
        [SerializeField] private MoonLaserShot laserShotPrefab;
        [SerializeField] private MoonTelegraphLine telegraphLinePrefab;
        [SerializeField] private float rockBossDamage = 95f;
        [SerializeField] private float rockPlayerDamage = 28f;
        [SerializeField] private float rockExplosionRadius = 1.4f;
        [SerializeField] private float spawnedRockLifeTime = 12f;
        [SerializeField] private Vector2 rockScaleRange = new Vector2(0.55f, 1.65f);

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
        protected override float PhaseTransitionDelay => phaseTransitionDuration;

        private int lastAttackIndex = -1;
        private Vector3 floatingOrigin;
        private Sequence floatingTween;

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
            StartAmbientFloating();
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
            MoonRock rock = Instantiate(rockPrefab, position, Quaternion.identity);
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
                rockBossDamage * (strong ? 1.35f : 1f),
                rockPlayerDamage * (strong ? 1.25f : 1f),
                rockExplosionRadius * scaleMultiplier * (strong ? 1.3f : 1f),
                spawnedRockLifeTime,
                playerLayer
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
            MoonHazardProjectile projectile = Instantiate(
                hazardProjectilePrefab,
                position,
                Quaternion.identity
            );
            projectile.Initialize(
                velocity,
                moveMode,
                homingTarget,
                damage,
                lifeTime,
                playerLayer,
                hazardGroundLayer,
                sprite,
                color
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
            MoonLaserShot shot = Instantiate(laserShotPrefab, origin, Quaternion.identity);
            shot.Initialize(
                this,
                origin,
                direction,
                length,
                width,
                warningDuration,
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
