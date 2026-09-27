using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Member.ODK._01_Script;
using Member.ODK.Scripts.Enemys.Bosses;
using UnityEngine;
using UnityEngine.Events;

namespace Member.ODK.Scripts.Enemys.Swordmaster
{
    public class Swordmaster : PhasedBossController, IDamageable
    {
        [Header("Collision")]
        [SerializeField] private LayerMask playerLayer = 1 << 6;
        [SerializeField] private LayerMask hazardGroundLayer = 1 << 3;
        [SerializeField] private Vector2 bodyHitboxSize = new Vector2(1.6f, 3.4f);
        [SerializeField] private Vector2 bodyHitboxOffset = new Vector2(0f, 1.7f);

        [Header("Eight Swords")]
        [SerializeField, Min(1)] private int swordCount = 8;
        [SerializeField] private EnchantedSword swordPrefab;
        [SerializeField] private SwordmasterTelegraph telegraphPrefab;
        [SerializeField] private Vector2 orbitRadius = new Vector2(4.3f, 1.9f);
        [SerializeField] private float orbitDepth = 1.15f;
        [SerializeField] private float orbitSpeed = 42f;
        [SerializeField] private float orbitHeight = 2.6f;
        [SerializeField] private float swordPlayerDamage = 24f;
        [SerializeField] private float swordBossDamage = 1f;

        [Header("Body Facing")]
        [SerializeField] private Transform bodyVisual;
        [SerializeField] private bool prefabFacesRight = true;
        [SerializeField] private bool faceTarget = true;

        [Header("Animation")]
        [SerializeField] private Animator bodyAnimator;
        [SerializeField] private float animationCrossFade = 0.02f;

        [Header("Camera Impulse")]
        [SerializeField] private float teleportImpulse = 0.2f;
        [SerializeField] private float summonSwordsImpulse = 0.25f;
        [SerializeField] private float dashReadyImpulse = 0.15f;
        [SerializeField] private float dashStartImpulse = 0.35f;
        [SerializeField] private float dashEndImpulse = 0.75f;
        [SerializeField] private float thrustGatherImpulse = 0f;
        [SerializeField] private float thrustStrikeImpulse = 0.58f;
        [SerializeField] private float crossfireReadyImpulse = 0.25f;
        [SerializeField] private float volleyWarningImpulse = 0f;
        [SerializeField] private float volleyFireImpulse = 0.36f;
        [SerializeField] private float finalCrossImpulse = 0.9f;
        [SerializeField] private float swordLaunchImpulse = 0f;
        [SerializeField] private float swordDispelledImpulse = 0f;
        [SerializeField] private float swordRecallImpulse = 0f;
        [SerializeField] private float swordImpactImpulse = 0.12f;
        [SerializeField] private float hitImpulse = 0.1f;
        [SerializeField] private float deathImpulse = 1.15f;

        [Header("Feedback")]
        [SerializeField] private SwordmasterFeedback feedback;

        [Header("Sword Summon")]
        [SerializeField] private float summonWindup = 0.25f;
        [SerializeField] private float summonTimeout = 1.2f;
        [SerializeField] private float summonRecover = 0.15f;

        [Header("Hooks")]
        [SerializeField] private BossPositionEvent onTeleport;
        [SerializeField] private BossPositionEvent onSwordLaunch;
        [SerializeField] private BossPositionEvent onSwordDispelled;
        [SerializeField] private BossPositionEvent onSwordRecalled;
        [SerializeField] private UnityEvent onDefeated;

        public LayerMask PlayerLayer => playerLayer;
        public LayerMask GroundLayer => hazardGroundLayer;
        public IReadOnlyList<EnchantedSword> Swords => swords;
        protected override bool HasPhaseTwo => false;

        private readonly List<EnchantedSword> swords = new List<EnchantedSword>();
        private readonly List<EnchantedSword> controlledSwords = new List<EnchantedSword>();
        private Vector3 bodyVisualScale;
        private int lastAttackIndex = -1;
        private bool isActing;
        private SpriteRenderer bodyRenderer;

        public const string IdleState = "Idle";
        public const string RunState = "Run";
        public const string Attack1State = "Attack1";
        public const string Attack2State = "Attack2";
        public const string JumpState = "Jump";
        public const string FallState = "Fall";
        public const string HurtState = "Hurt";
        public const string DeathState = "Death";

        protected override void Awake()
        {
            base.Awake();
            if (bodyAnimator == null && bodyVisual != null) bodyAnimator = bodyVisual.GetComponentInChildren<Animator>();
            if (feedback == null) feedback = GetComponent<SwordmasterFeedback>();
            if (feedback == null) feedback = gameObject.AddComponent<SwordmasterFeedback>();
            ConfigureBody();
            CreateSwords();
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
            int index;
            do index = Random.Range(0, 3);
            while (index == lastAttackIndex);
            lastAttackIndex = index;
            yield return PlayAttack(index);
        }

        private void LateUpdate()
        {
            if (!faceTarget || Target == null || bodyVisual == null) return;
            bool targetRight = Target.position.x >= transform.position.x;
            float direction = targetRight == prefabFacesRight ? 1f : -1f;
            Vector3 scale = bodyVisualScale;
            scale.x = Mathf.Abs(bodyVisualScale.x) * direction;
            bodyVisual.localScale = scale;
        }

        private void ConfigureBody()
        {
            CapsuleCollider2D hitbox = GetComponent<CapsuleCollider2D>();
            if (hitbox == null) hitbox = gameObject.AddComponent<CapsuleCollider2D>();
            hitbox.direction = CapsuleDirection2D.Vertical;
            hitbox.size = bodyHitboxSize;
            hitbox.offset = bodyHitboxOffset;
            if (bodyVisual != null) bodyVisualScale = bodyVisual.localScale;
        }

        public void PlayAnimation(string stateName, bool restart = true)
        {
            if (bodyAnimator == null || string.IsNullOrWhiteSpace(stateName)) return;
            if (IsDead && stateName != DeathState) return;
            int hash = Animator.StringToHash(stateName);
            if (!bodyAnimator.HasState(0, hash)) return;
            if (!restart && bodyAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash == hash) return;
            bodyAnimator.CrossFadeInFixedTime(hash, animationCrossFade, 0, 0f);
        }

        public void SetActing(bool acting)
        {
            isActing = acting;
            if (!acting && !IsDead && IsPlayingState(RunState)) PlayAnimation(IdleState);
        }

        public IEnumerator SummonSwords(float speedScale = 1f)
        {
            speedScale = Mathf.Max(0.01f, speedScale);
            bool needed = false;
            foreach (EnchantedSword sword in swords)
                if (sword != null && sword.CanBeSummoned) needed = true;
            if (!needed || IsDead) yield break;

            PlayAnimation(Attack1State);
            Cue(SwordmasterCue.SummonSwords, (Vector3)GetHitCenter());
            yield return new WaitForSeconds(summonWindup / speedScale);

            foreach (EnchantedSword sword in swords)
                if (sword != null) sword.Summon();

            float elapsed = 0f;
            while (elapsed < summonTimeout && !IsDead && AnySwordReturning())
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            yield return new WaitForSeconds(summonRecover / speedScale);
        }

        private bool AnySwordReturning()
        {
            foreach (EnchantedSword sword in swords)
                if (sword != null && (sword.IsReturning || sword.CanBeSummoned)) return true;
            return false;
        }

        public int BodySortingOrder
        {
            get
            {
                if (bodyRenderer == null && bodyVisual != null) bodyRenderer = bodyVisual.GetComponentInChildren<SpriteRenderer>();
                return bodyRenderer != null ? bodyRenderer.sortingOrder : 10;
            }
        }

        public Vector2 GetHitCenter()
        {
            return (Vector2)transform.position + bodyHitboxOffset;
        }

        public bool IsPlayingState(string stateName)
        {
            if (bodyAnimator == null) return false;
            return bodyAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash == Animator.StringToHash(stateName);
        }

        public void Cue(SwordmasterCue cue, Vector3 position)
        {
            feedback?.Play(cue, position);
            float impulse = GetImpulse(cue);
            if (impulse > 0f) ShakeCamera(impulse);
        }

        private float GetImpulse(SwordmasterCue cue)
        {
            switch (cue)
            {
                case SwordmasterCue.Teleport: return teleportImpulse;
                case SwordmasterCue.SummonSwords: return summonSwordsImpulse;
                case SwordmasterCue.DashReady: return dashReadyImpulse;
                case SwordmasterCue.DashStart: return dashStartImpulse;
                case SwordmasterCue.DashEnd: return dashEndImpulse;
                case SwordmasterCue.ThrustGather: return thrustGatherImpulse;
                case SwordmasterCue.ThrustStrike: return thrustStrikeImpulse;
                case SwordmasterCue.CrossfireReady: return crossfireReadyImpulse;
                case SwordmasterCue.VolleyWarning: return volleyWarningImpulse;
                case SwordmasterCue.VolleyFire: return volleyFireImpulse;
                case SwordmasterCue.FinalCross: return finalCrossImpulse;
                case SwordmasterCue.SwordLaunch: return swordLaunchImpulse;
                case SwordmasterCue.SwordDispelled: return swordDispelledImpulse;
                case SwordmasterCue.SwordRecall: return swordRecallImpulse;
                case SwordmasterCue.SwordImpact: return swordImpactImpulse;
                case SwordmasterCue.Hit: return hitImpulse;
                case SwordmasterCue.Death: return deathImpulse;
                default: return 0f;
            }
        }

        private void CreateSwords()
        {
            for (int i = 0; i < swordCount; i++)
            {
                if (swordPrefab == null) return;
                EnchantedSword sword = Instantiate(swordPrefab, transform);
                sword.name = $"Enchanted Sword {i + 1}";
                sword.Initialize(this, i, swordPlayerDamage, swordBossDamage, playerLayer, hazardGroundLayer);
                swords.Add(sword);
            }
        }

        public Vector3 GetOrbitPosition(int index, float angleOffset = 0f)
        {
            float angle = Time.time * orbitSpeed + 360f * index / Mathf.Max(1, swordCount) + angleOffset;
            float radians = angle * Mathf.Deg2Rad;
            return transform.position + new Vector3(
                Mathf.Cos(radians) * orbitRadius.x,
                orbitHeight + Mathf.Sin(radians) * orbitRadius.y,
                Mathf.Sin(radians) * orbitDepth
            );
        }

        public float GetOrbitAngle(int index) =>
            Time.time * orbitSpeed + 360f * index / Mathf.Max(1, swordCount);

        public List<EnchantedSword> TakeSwords(int count)
        {
            controlledSwords.Clear();
            foreach (EnchantedSword sword in swords)
            {
                if (controlledSwords.Count >= count) break;
                if (sword == null || !sword.CanBossControl) continue;
                sword.BeginBossControl();
                controlledSwords.Add(sword);
            }

            if (controlledSwords.Count < count)
            {
                List<EnchantedSword> reclaimable = new List<EnchantedSword>();
                foreach (EnchantedSword sword in swords)
                    if (sword != null && sword.CanBossReclaim) reclaimable.Add(sword);
                Vector3 center = transform.position;
                reclaimable.Sort((a, b) =>
                    (b.transform.position - center).sqrMagnitude.CompareTo((a.transform.position - center).sqrMagnitude));
                foreach (EnchantedSword sword in reclaimable)
                {
                    if (controlledSwords.Count >= count) break;
                    sword.BeginBossControl();
                    controlledSwords.Add(sword);
                }
            }
            return new List<EnchantedSword>(controlledSwords);
        }

        public void ReturnControlledSwords()
        {
            foreach (EnchantedSword sword in controlledSwords)
                sword?.EndBossControl();
            controlledSwords.Clear();
        }

        public void ReturnEverySword()
        {
            foreach (EnchantedSword sword in swords)
                sword?.Recall();
            controlledSwords.Clear();
        }

        public SwordmasterTelegraph SpawnTelegraph()
        {
            return telegraphPrefab != null ? ODKPool.Spawn(telegraphPrefab, Vector3.zero, Quaternion.identity) : null;
        }

        public void ReleaseTelegraph(SwordmasterTelegraph telegraph)
        {
            if (telegraph == null) return;
            telegraph.Hide();
            ODKPool.Despawn(telegraph);
        }

        public void NotifySwordLaunch(Vector3 position)
        {
            Cue(SwordmasterCue.SwordLaunch, position);
            onSwordLaunch?.Invoke(position);
        }

        public void NotifySwordDispelled(Vector3 position)
        {
            Cue(SwordmasterCue.SwordDispelled, position);
            onSwordDispelled?.Invoke(position);
        }

        public void NotifySwordRecalled(Vector3 position)
        {
            Cue(SwordmasterCue.SwordRecall, position);
            onSwordRecalled?.Invoke(position);
        }

        public void NotifySwordImpact(Vector3 position)
        {
            Cue(SwordmasterCue.SwordImpact, position);
        }

        public void Teleport(Vector3 destination)
        {
            transform.DOKill();
            destination = Arena != null ? Arena.Clamp(destination, 1f) : destination;
            transform.position = destination;
            PlayAnimation(FallState);
            Cue(SwordmasterCue.Teleport, destination);
            onTeleport?.Invoke(destination);
        }

        public void StopMotion()
        {
            transform.DOKill();
            if (TryGetComponent(out Rigidbody2D body)) body.linearVelocity = Vector2.zero;
        }

        public void TakeDamage(DamageData damage)
        {
            if (IsDead) return;
            Cue(SwordmasterCue.Hit, transform.position + Vector3.up * bodyHitboxOffset.y);
            ApplyBossDamage(damage);
            if (!isActing && !IsDead) PlayAnimation(HurtState);
        }

        protected override void OnPhaseTwoEntered() { }

        protected override void OnBossDeath()
        {
            StopMotion();
            ReturnEverySword();
            isActing = false;
            PlayAnimation(DeathState);
            Cue(SwordmasterCue.Death, transform.position);
            onDefeated?.Invoke();
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = new Color(0.75f, 0.2f, 1f, 0.85f);
            Vector3 center = transform.position + Vector3.up * orbitHeight;
            const int segments = 48;
            Vector3 previous = center + Vector3.right * orbitRadius.x;
            for (int i = 1; i <= segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                Vector3 next = center + new Vector3(
                    Mathf.Cos(angle) * orbitRadius.x,
                    Mathf.Sin(angle) * orbitRadius.y,
                    Mathf.Sin(angle) * orbitDepth
                );
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
