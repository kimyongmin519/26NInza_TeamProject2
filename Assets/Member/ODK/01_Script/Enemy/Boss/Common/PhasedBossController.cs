using System;
using System.Collections;
using System.Collections.Generic;
using Member.ODK.Scripts.Enemys.Skills;
using UnityEngine;
using UnityEngine.Events;

namespace Member.ODK.Scripts.Enemys.Bosses
{
    public abstract class PhasedBossController : EnemyController
    {
        [Header("Attack")]
        [SerializeField] private float attackInterval = 1f;
        [SerializeField] private bool playOnStart = true;

        [Header("Target / Ground")]
        [SerializeField] private Transform target;
        [SerializeField] private LayerMask groundLayer = 1 << 3;
        [field: SerializeField] public Transform ArenaPos { get; private set; }
        [field: SerializeField] public float ArenaHalfWidth { get; private set; } = 13.5f;
        [SerializeField] private float groundRayHeight = 20f;
        [SerializeField] private float groundRayDistance = 50f;
        [SerializeField] private bool drawDebugGizmos = true;

        [Header("Health / Phase")]
        [SerializeField] private HealthModule healthModule;
        [SerializeField] private DeathModule deathModule;
        [SerializeField] private bool invincible;

        [Header("Grab Rock")]
        [SerializeField] private GameObject[] rockVisualPrefabs;
        [SerializeField] private int fistRockCount = 1;
        [SerializeField] private int sawRockCount = 4;
        [SerializeField] private float rockSpawnSpread = 1.2f;
        [SerializeField] private float rockSpawnHeight = 0.15f;
        [SerializeField] private float rockLaunchForce = 5f;
        [SerializeField] private float rockScale = 1.3f;
        [SerializeField] private float rockDamage = 80f;
        [SerializeField] private float rockLifeTime = 15f;

        [Header("Effect Hook")]
        [SerializeField] private BossPositionEvent onAttackReady;
        [SerializeField] private BossPositionEvent onAttackImpact;
        [SerializeField] private BossPositionEvent onMissileSpawn;
        [SerializeField] private BossPositionEvent onRockSpawn;
        [SerializeField] private BossPositionEvent onRockBreak;
        [SerializeField] private UnityEvent onPhaseTwo;
        [SerializeField] private UnityEvent onDeath;

        public Transform Target => target;
        public Vector3 ArenaCenter => ArenaPos != null ? ArenaPos.position : (Vector3)originPos;
        public Vector2 originPos { get; private set; }
        public float CurrentHealth => healthModule != null ? healthModule.CurrentHealth : 0f;
        public float MaxHealth => healthModule != null ? healthModule.MaxHealth : 0f;
        public bool IsPhaseTwo { get; private set; }
        public bool IsDead { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            originPos = transform.position;
            if (healthModule == null) healthModule = GetModule<HealthModule>();
            if (deathModule == null) deathModule = GetModule<DeathModule>();
            if (deathModule != null) deathModule.OnDeath += HandleHealthDeath;
        }

        protected virtual void Start()
        {
            FindTarget();
            InitializeAttacks();
            if (playOnStart) StartCoroutine(AttackLoop());
        }

        protected abstract IEnumerable<BossSkill> GetAttacks();
        protected abstract IEnumerator PhaseOneLoop();
        protected abstract IEnumerator PhaseTwoLoop();

        protected virtual void OnPhaseTwoEntered() { }
        protected virtual void OnBossDeath() { }
        protected virtual void OnAttackReady(Vector3 position) { }
        protected virtual void OnAttackImpact(Vector3 position) { }
        protected virtual void OnMissileSpawn(Vector3 position) { }

        private void InitializeAttacks()
        {
            foreach (BossSkill attack in GetAttacks())
                attack?.InitializeAttack(this);
        }

        private IEnumerator AttackLoop()
        {
            yield return new WaitUntil(FindTarget);
            yield return AttackWait();

            while (!IsDead)
            {
                if (IsPhaseTwo) yield return PhaseTwoLoop();
                else yield return PhaseOneLoop();
            }
        }

        protected IEnumerator PlayAttack(BossSkill attack, float durationScale = 1f)
        {
            if (attack != null) yield return attack.Play(durationScale);
        }

        protected IEnumerator RunParallel(
            BossSkill first,
            float firstScale,
            BossSkill second,
            float secondScale)
        {
            int runningCount = 2;
            StartCoroutine(RunAttack(first, firstScale, () => runningCount--));
            StartCoroutine(RunAttack(second, secondScale, () => runningCount--));
            yield return new WaitUntil(() => runningCount <= 0 || IsDead);
        }

        private IEnumerator RunAttack(BossSkill attack, float durationScale, Action onComplete)
        {
            yield return PlayAttack(attack, durationScale);
            onComplete?.Invoke();
        }

        protected WaitForSeconds AttackWait()
        {
            return new WaitForSeconds(attackInterval);
        }

        protected bool FindTarget()
        {
            if (target != null) return true;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
            return target != null;
        }

        public Vector3 GetGroundPoint(float x)
        {
            float targetY = target != null ? target.position.y : ArenaCenter.y;
            float startY = Mathf.Max(targetY, ArenaCenter.y) + groundRayHeight;
            RaycastHit2D hit = Physics2D.Raycast(
                new Vector2(x, startY),
                Vector2.down,
                groundRayDistance,
                groundLayer
            );
            return hit.collider != null
                ? (Vector3)hit.point
                : new Vector3(x, ArenaCenter.y, 0f);
        }

        public Vector3 GetImpactVisualPosition(
            Transform piece,
            Vector3 groundPoint,
            Vector2 visualOffset)
        {
            Vector3 impactPosition = groundPoint + (Vector3)visualOffset;
            if (piece == null) return impactPosition;

            impactPosition.y += GetGroundOffset(piece);
            impactPosition.z = piece.position.z;
            return impactPosition;
        }

        protected static float GetGroundOffset(Transform piece)
        {
            if (piece == null) return 0f;

            Collider2D pieceCollider = piece.GetComponentInChildren<Collider2D>();
            if (pieceCollider != null)
                return Mathf.Max(0f, piece.position.y - pieceCollider.bounds.min.y);

            SpriteRenderer pieceRenderer = piece.GetComponentInChildren<SpriteRenderer>();
            return pieceRenderer != null
                ? Mathf.Max(0f, piece.position.y - pieceRenderer.bounds.min.y)
                : 0f;
        }

        public void AttackReady(Vector3 position)
        {
            onAttackReady?.Invoke(position);
            OnAttackReady(position);
        }

        public void AttackImpact(Vector3 position)
        {
            onAttackImpact?.Invoke(position);
            OnAttackImpact(position);
        }

        public void MissileSpawn(Vector3 position)
        {
            onMissileSpawn?.Invoke(position);
            OnMissileSpawn(position);
        }

        public void SpawnFistRocks(Vector3 position) => SpawnRocks(position, fistRockCount);
        public void SpawnSawRocks(Vector3 position) => SpawnRocks(position, sawRockCount);

        private void SpawnRocks(Vector3 groundPoint, int count)
        {
            if (rockVisualPrefabs == null || rockVisualPrefabs.Length == 0 || count <= 0)
                return;

            for (int i = 0; i < count; i++)
            {
                GameObject visualPrefab = rockVisualPrefabs[UnityEngine.Random.Range(0, rockVisualPrefabs.Length)];
                if (visualPrefab == null) continue;

                float centerRate = count <= 1 ? 0f : (float)i / (count - 1) - 0.5f;
                Vector3 spawnPosition = groundPoint + Vector3.up * rockSpawnHeight;
                GameObject rockObject = Instantiate(
                    visualPrefab,
                    spawnPosition,
                    Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-25f, 25f))
                );
                rockObject.name = name + " Rock";
                int propLayer = LayerMask.NameToLayer("Prop");
                if (propLayer >= 0) rockObject.layer = propLayer;
                rockObject.transform.localScale *= rockScale;

                if (!rockObject.TryGetComponent(out Rigidbody2D rockRigidbody))
                    rockRigidbody = rockObject.AddComponent<Rigidbody2D>();
                rockRigidbody.mass = 0.7f;
                rockRigidbody.gravityScale = 2f;
                rockRigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                if (!rockObject.TryGetComponent(out Collider2D _))
                {
                    CircleCollider2D rockCollider = rockObject.AddComponent<CircleCollider2D>();
                    SpriteRenderer rockRenderer = rockObject.GetComponent<SpriteRenderer>();
                    if (rockRenderer != null && rockRenderer.sprite != null)
                    {
                        rockCollider.radius = Mathf.Max(
                            rockRenderer.sprite.bounds.extents.x,
                            rockRenderer.sprite.bounds.extents.y
                        ) * 0.75f;
                        rockRenderer.sortingOrder = 15;
                    }
                }

                Vector2 launchDirection = new Vector2(
                    centerRate * rockSpawnSpread + UnityEngine.Random.Range(-0.25f, 0.25f),
                    1f
                ).normalized;
                Vector2 launchVelocity = launchDirection * UnityEngine.Random.Range(
                    rockLaunchForce * 0.75f,
                    rockLaunchForce * 1.25f
                );

                ConfigureRock(
                    rockObject,
                    rockRigidbody,
                    launchVelocity,
                    UnityEngine.Random.Range(-240f, 240f),
                    rockDamage,
                    rockLifeTime
                );
                onRockSpawn?.Invoke(spawnPosition);
            }
        }

        protected virtual void ConfigureRock(
            GameObject rockObject,
            Rigidbody2D rockRigidbody,
            Vector2 launchVelocity,
            float angularVelocity,
            float damage,
            float lifeTime)
        {
            rockRigidbody.linearVelocity = launchVelocity;
            rockRigidbody.angularVelocity = angularVelocity;
            Destroy(rockObject, lifeTime);
        }

        protected void RockBreak(Vector3 position)
        {
            onRockBreak?.Invoke(position);
        }

        protected void ApplyBossDamage(DamageData damage)
        {
            if (IsDead || invincible || healthModule == null) return;
            healthModule.ApplyDamage(damage);
        }

        private void HandleHealthDeath()
        {
            if (IsDead) return;
            if (IsPhaseTwo)
            {
                Die();
                return;
            }

            EnterPhaseTwo();
            if (deathModule != null) deathModule.Revive();
            else if (healthModule != null) healthModule.SetMaxHealth(healthModule.MaxHealth, true);
        }

        [ContextMenu("Enter Phase Two")]
        public void EnterPhaseTwo()
        {
            if (IsPhaseTwo || IsDead) return;

            IsPhaseTwo = true;
            StopAllCoroutines();
            CancelAttacks();
            OnPhaseTwoEntered();
            onPhaseTwo?.Invoke();
            StartCoroutine(PhaseTwoRestart());
        }

        private IEnumerator PhaseTwoRestart()
        {
            yield return new WaitForSeconds(1.1f);
            while (!IsDead && IsPhaseTwo)
                yield return PhaseTwoLoop();
        }

        protected void CancelAttacks()
        {
            foreach (BossSkill attack in GetAttacks())
                attack?.StopSkill();
        }

        protected void Die()
        {
            if (IsDead) return;

            IsDead = true;
            StopAllCoroutines();
            CancelAttacks();
            OnBossDeath();
            onDeath?.Invoke();
        }

        protected virtual void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos) return;

            Vector3 center = ArenaPos != null ? ArenaPos.position : transform.position;
            Gizmos.color = new Color(0.1f, 0.9f, 1f, 0.8f);
            Gizmos.DrawLine(
                center + Vector3.left * ArenaHalfWidth,
                center + Vector3.right * ArenaHalfWidth
            );
            Gizmos.DrawWireCube(center, new Vector3(ArenaHalfWidth * 2f, 0.2f, 0f));

            Gizmos.color = new Color(1f, 0.9f, 0.1f, 0.75f);
            for (int i = 0; i < 5; i++)
            {
                float rate = i / 4f;
                float x = Mathf.Lerp(
                    center.x - ArenaHalfWidth * 0.75f,
                    center.x + ArenaHalfWidth * 0.75f,
                    rate
                );
                float startY = center.y + groundRayHeight;
                RaycastHit2D hit = Physics2D.Raycast(
                    new Vector2(x, startY),
                    Vector2.down,
                    groundRayDistance,
                    groundLayer
                );
                Vector3 end = hit.collider != null
                    ? (Vector3)hit.point
                    : new Vector3(x, startY - groundRayDistance, 0f);
                Gizmos.DrawLine(new Vector3(x, startY, 0f), end);
                Gizmos.DrawWireSphere(end, 0.22f);
            }

            if (target != null)
            {
                Vector3 groundPoint = GetGroundPoint(target.position.x);
                Gizmos.color = new Color(1f, 0.45f, 0f, 0.85f);
                Gizmos.DrawWireSphere(groundPoint, rockSpawnSpread);
                Gizmos.DrawLine(groundPoint, groundPoint + Vector3.up * rockSpawnHeight);
            }
        }

        protected virtual void OnDestroy()
        {
            if (deathModule != null) deathModule.OnDeath -= HandleHealthDeath;
            CancelAttacks();
        }
    }
}
