using System.Collections;
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
        [SerializeField] private float rockBossDamage = 95f;
        [SerializeField] private float rockPlayerDamage = 28f;
        [SerializeField] private float rockExplosionRadius = 1.4f;
        [SerializeField] private float spawnedRockLifeTime = 12f;

        [Header("Animation")]
        [SerializeField] private Animator moonAnimator;
        [SerializeField] private string phaseTwoAnimationState;
        [SerializeField] private float phaseTransitionDuration = 1f;

        [Header("Camera Impulse")]
        [SerializeField] private CinemachineImpulseSource impulseSource;
        [SerializeField] private float normalImpactImpulse = 1.2f;
        [SerializeField] private float strongImpactImpulse = 2.4f;

        [Header("Feedback")]
        [SerializeField] private BossPositionEvent onDamaged;
        [SerializeField] private UnityEvent onPhaseTwoVisual;

        public LayerMask PlayerLayer => playerLayer;
        public LayerMask GroundLayer => hazardGroundLayer;
        protected override float PhaseTransitionDelay => phaseTransitionDuration;

        private int lastAttackIndex = -1;

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
            if (impulseSource == null) impulseSource = GetComponent<CinemachineImpulseSource>();
            if (impulseSource == null) impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
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

        public MoonRock SpawnRock(Vector3 position, Vector2 velocity, bool strong)
        {
            MoonRock rock = rockPrefab != null
                ? Instantiate(rockPrefab, position, Quaternion.identity)
                : MoonRock.CreateFallback(position);
            rock.Initialize(
                this,
                velocity,
                rockBossDamage * (strong ? 1.35f : 1f),
                rockPlayerDamage * (strong ? 1.25f : 1f),
                rockExplosionRadius * (strong ? 1.3f : 1f),
                spawnedRockLifeTime,
                playerLayer,
                hazardGroundLayer
            );
            return rock;
        }

        public void ShakeImpact(bool strong)
        {
            if (impulseSource == null) return;
            impulseSource.GenerateImpulse(strong ? strongImpactImpulse : normalImpactImpulse);
        }

        public void TakeDamage(DamageData damage)
        {
            if (IsDead) return;
            onDamaged?.Invoke(transform.position);
            ApplyBossDamage(damage);
        }

        protected override void OnPhaseTwoEntered()
        {
            PlayAnimation(phaseTwoAnimationState);
            onPhaseTwoVisual?.Invoke();
        }
    }
}
