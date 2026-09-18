using System.Collections;
using System.Collections.Generic;
using Member.ODK._01_Script;
using Member.ODK.Scripts.Enemys.Bosses;
using UnityEngine;
using UnityEngine.Events;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    public class MoonBoss : PhasedBossController, IDamageable
    {
        [Header("Pattern")]
        [SerializeField] private MoonMeteorAttack meteorAttack;
        [SerializeField] private MoonOrbitAttack orbitAttack;
        [SerializeField] private MoonReflectionAttack reflectionAttack;
        [SerializeField] private LayerMask playerLayer = 1 << 6;

        [Header("Moon Phase Clone")]
        [SerializeField] private GameObject phaseClonePrefab;
        [SerializeField] private UnityEvent onPhaseCloneSpawn;
        [SerializeField] private UnityEvent onPhaseCloneDespawn;

        [Header("Animation Slots")]
        [SerializeField] private Animator moonAnimator;
        [SerializeField] private string backsideAnimationState;
        [SerializeField] private float phaseTransitionDuration = 1.1f;
        [SerializeField] private string gravityAnimationState;
        [SerializeField] private float gravityInterval = 12f;
        [SerializeField] private float gravityAnimationDuration = 1.2f;
        [SerializeField] private UnityEvent onGravityAnimationStart;
        [SerializeField] private UnityEvent onGravityAnimationEnd;

        [Header("Damage Feedback")]
        [SerializeField] private BossPositionEvent onDamaged;

        [Header("Shockwave Visual")]
        [SerializeField] private MoonShockwaveEffect shockwavePrefab;
        [SerializeField] private Material shockwaveMaterial;
        [SerializeField] private float shockwaveVisualDuration = 0.55f;

        public LayerMask PlayerLayer => playerLayer;
        public Transform PhaseClone => phaseClone != null ? phaseClone.transform : null;

        protected override float PhaseTransitionDelay => phaseTransitionDuration;

        private readonly List<MoonSkill> availableAttacks = new List<MoonSkill>();
        private GameObject phaseClone;
        private int completedPatternCount;
        private int lastPatternIndex = -1;
        private float nextGravityTime;

        protected override void Awake()
        {
            base.Awake();
            if (moonAnimator == null) moonAnimator = GetComponentInChildren<Animator>();
            PrepareAttacks();
        }

        private void PrepareAttacks()
        {
            if (meteorAttack == null) meteorAttack = GetComponentInChildren<MoonMeteorAttack>(true);
            if (orbitAttack == null) orbitAttack = GetComponentInChildren<MoonOrbitAttack>(true);
            if (reflectionAttack == null) reflectionAttack = GetComponentInChildren<MoonReflectionAttack>(true);

            if (meteorAttack == null) meteorAttack = gameObject.AddComponent<MoonMeteorAttack>();
            if (orbitAttack == null) orbitAttack = gameObject.AddComponent<MoonOrbitAttack>();
            if (reflectionAttack == null) reflectionAttack = gameObject.AddComponent<MoonReflectionAttack>();

            availableAttacks.Clear();
            availableAttacks.Add(meteorAttack);
            availableAttacks.Add(orbitAttack);
            availableAttacks.Add(reflectionAttack);
        }

        protected override IEnumerable<ODKBossSkill> GetAttacks()
        {
            foreach (MoonSkill attack in availableAttacks)
                yield return attack;
        }

        protected override IEnumerator PhaseOneLoop()
        {
            yield return PlayNextPattern();
            yield return AttackWait();
        }

        protected override IEnumerator PhaseTwoLoop()
        {
            if (Time.time >= nextGravityTime)
            {
                yield return PlayGravityAnimationSlot();
                nextGravityTime = Time.time + Mathf.Max(0.1f, gravityInterval);
            }

            yield return PlayNextPattern();
            yield return AttackWait();
        }

        private IEnumerator PlayNextPattern()
        {
            MoonSkill attack = SelectRandomAttack();
            if (attack == null) yield break;

            bool useClone = (completedPatternCount + 1) % 4 == 0;
            if (useClone) SpawnPhaseClone();

            yield return PlayAttack(attack);
            completedPatternCount++;

            if (useClone) DespawnPhaseClone();
        }

        private MoonSkill SelectRandomAttack()
        {
            if (availableAttacks.Count == 0) return null;
            if (availableAttacks.Count == 1) return availableAttacks[0];

            int index;
            do
            {
                index = Random.Range(0, availableAttacks.Count);
            }
            while (index == lastPatternIndex);

            lastPatternIndex = index;
            return availableAttacks[index];
        }

        private void SpawnPhaseClone()
        {
            DespawnPhaseClone();

            Vector3 clonePosition = GetOppositePosition(transform.position);
            phaseClone = phaseClonePrefab != null
                ? Instantiate(phaseClonePrefab, clonePosition, transform.rotation)
                : CreateFallbackClone(clonePosition);

            phaseClone.name = "Moon Phase Clone";
            onPhaseCloneSpawn?.Invoke();
        }

        private GameObject CreateFallbackClone(Vector3 position)
        {
            GameObject clone = new GameObject("Moon Phase Clone");
            clone.transform.SetPositionAndRotation(position, transform.rotation);
            clone.transform.localScale = transform.lossyScale;

            SpriteRenderer source = GetComponentInChildren<SpriteRenderer>();
            if (source != null)
            {
                SpriteRenderer renderer = clone.AddComponent<SpriteRenderer>();
                renderer.sprite = source.sprite;
                renderer.sharedMaterial = source.sharedMaterial;
                renderer.color = new Color(source.color.r, source.color.g, source.color.b, 0.65f);
                renderer.flipX = source.flipX;
                renderer.flipY = source.flipY;
                renderer.sortingLayerID = source.sortingLayerID;
                renderer.sortingOrder = source.sortingOrder;
            }

            return clone;
        }

        public IEnumerable<Transform> GetPatternOrigins()
        {
            yield return transform;
            if (phaseClone != null) yield return phaseClone.transform;
        }

        public Vector3 GetOppositePosition(Vector3 position)
        {
            Vector3 center = ArenaCenter;
            return new Vector3(
                center.x * 2f - position.x,
                center.y * 2f - position.y,
                position.z
            );
        }

        public void PlayAnimation(string stateName)
        {
            PlayAnimation(moonAnimator, stateName);
            if (phaseClone != null)
                PlayAnimation(phaseClone.GetComponentInChildren<Animator>(), stateName);
        }

        private static void PlayAnimation(Animator animator, string stateName)
        {
            if (animator == null || string.IsNullOrWhiteSpace(stateName)) return;
            int stateHash = Animator.StringToHash(stateName);
            if (animator.HasState(0, stateHash))
                animator.CrossFade(stateHash, 0.08f, 0, 0f);
        }

        private IEnumerator PlayGravityAnimationSlot()
        {
            PlayAnimation(gravityAnimationState);
            onGravityAnimationStart?.Invoke();
            yield return new WaitForSeconds(Mathf.Max(0f, gravityAnimationDuration));
            onGravityAnimationEnd?.Invoke();
        }

        private void DespawnPhaseClone()
        {
            if (phaseClone == null) return;
            Destroy(phaseClone);
            phaseClone = null;
            onPhaseCloneDespawn?.Invoke();
        }

        public void TakeDamage(DamageData damage)
        {
            if (IsDead) return;
            onDamaged?.Invoke(transform.position);
            ApplyBossDamage(damage);
        }

        public void PlayShockwave(Vector3 position, float radius, Color color)
        {
            if (shockwavePrefab != null)
            {
                MoonShockwaveEffect effect = Instantiate(shockwavePrefab, position, Quaternion.identity);
                effect.Play(radius, color, shockwaveMaterial);
                return;
            }

            MoonShockwaveEffect.Spawn(
                position,
                radius,
                color,
                shockwaveMaterial,
                shockwaveVisualDuration
            );
        }

        protected override void OnPhaseTwoEntered()
        {
            DespawnPhaseClone();
            PlayAnimation(backsideAnimationState);
            nextGravityTime = Time.time + Mathf.Max(0.1f, gravityInterval);
        }

        protected override void OnBossDeath()
        {
            DespawnPhaseClone();
        }

        protected override void OnDestroy()
        {
            DespawnPhaseClone();
            base.OnDestroy();
        }
    }
}
