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
        [SerializeField] private Vector2 orbitRadius = new Vector2(3.2f, 1.45f);
        [SerializeField] private float orbitDepth = 1.15f;
        [SerializeField] private float orbitSpeed = 42f;
        [SerializeField] private float orbitHeight = 2.1f;
        [SerializeField] private float swordPlayerDamage = 24f;
        [SerializeField] private float swordBossDamage = 70f;

        [Header("Body Facing")]
        [SerializeField] private Transform bodyVisual;
        [SerializeField] private bool prefabFacesRight = true;
        [SerializeField] private bool faceTarget = true;

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

        protected override void Awake()
        {
            base.Awake();
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
                if (sword == null || !sword.CanBossControl) continue;
                sword.BeginBossControl();
                controlledSwords.Add(sword);
                if (controlledSwords.Count >= count) break;
            }
            return new List<EnchantedSword>(controlledSwords);
        }

        public void ReturnControlledSwords()
        {
            foreach (EnchantedSword sword in controlledSwords)
                sword?.Recall();
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
            return telegraphPrefab != null ? Instantiate(telegraphPrefab) : null;
        }

        public void NotifySwordLaunch(Vector3 position) => onSwordLaunch?.Invoke(position);
        public void NotifySwordDispelled(Vector3 position) => onSwordDispelled?.Invoke(position);
        public void NotifySwordRecalled(Vector3 position) => onSwordRecalled?.Invoke(position);

        public void Teleport(Vector3 destination)
        {
            transform.DOKill();
            destination = Arena != null ? Arena.Clamp(destination, 1f) : destination;
            transform.position = destination;
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
            ApplyBossDamage(damage);
        }

        protected override void OnPhaseTwoEntered() { }

        protected override void OnBossDeath()
        {
            StopMotion();
            ReturnEverySword();
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
