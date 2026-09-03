using DG.Tweening;
using Member.ODK.Scripts.Enemys;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public class Volcanus : EnemyController
    {
        [Header("Piece")]
        [field: SerializeField] public VolcanusPiece Head { get; private set; }
        [field: SerializeField] public VolcanusPiece Truso { get; private set; }
        [field: SerializeField] public VolcanusPiece LeftHand { get; private set; }
        [field: SerializeField] public VolcanusPiece RightHand { get; private set; }
        [field: SerializeField] public Transform ArenaPos { get; private set; }
        [field: SerializeField] public Transform Saw { get; private set; }
        [field: SerializeField] public float SawDownAngle { get; private set; } = -42f;
        [field: SerializeField] public float FistDownAngle { get; private set; }

        [Header("Attack")]
        [SerializeField] private VolcanusPunchAttack rightPunchAttack;
        [SerializeField] private VolcanusSawSlamAttack sawSlamAttack;
        [SerializeField] private VolcanusPunchAttack leftPunchAttack;
        [SerializeField] private VolcanusSideSlashAttack sideSlashAttack;
        [SerializeField] private VolcanusFiveSlamAttack fiveSlamAttack;
        [SerializeField] private VolcanusLaserAttack laserAttack;
        [SerializeField] private VolcanusMissileAttack missileAttack;
        [SerializeField] private VolcanusFeedback feedback;
        [SerializeField] private float attackInterval = 1f;
        [SerializeField] private bool playOnStart = true;

        [Header("Target / Ground")]
        [SerializeField] private Transform target;
        [SerializeField] private LayerMask groundLayer = 1 << 3;
        [field: SerializeField] public float ArenaHalfWidth { get; private set; } = 13.5f;
        [SerializeField] private float groundRayHeight = 20f;
        [SerializeField] private float groundRayDistance = 50f;
        [SerializeField] private Vector2 sawImpactVisualOffset = new Vector2(0f, 0.1f);
        [field: SerializeField] public Vector2 FistImpactVisualOffset { get; private set; } = new Vector2(0f, -0.15f);
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
        [SerializeField] private VolcanusPositionEvent onAttackReady;
        [SerializeField] private VolcanusPositionEvent onAttackImpact;
        [SerializeField] private VolcanusPositionEvent onMissileSpawn;
        [SerializeField] private VolcanusPositionEvent onRockSpawn;
        [SerializeField] private VolcanusPositionEvent onRockBreak;
        [SerializeField] private VolcanusPositionEvent onWeakPointHit;
        [SerializeField] private VolcanusPositionEvent onBlockedHit;
        [SerializeField] private UnityEvent onPhaseTwo;
        [SerializeField] private UnityEvent onDeath;

        public Transform Target => target;
        public Transform BodyRoot { get; private set; }
        public Vector3 BodyOriginLocalPosition { get; private set; }
        public Vector3 ArenaCenter => ArenaPos != null ? ArenaPos.position : (Vector3)originPos;
        public Vector2 originPos { get; private set; }
        public float CurrentHealth => healthModule != null ? healthModule.CurrentHealth : 0f;
        public float MaxHealth => healthModule != null ? healthModule.MaxHealth : 0f;
        public bool IsPhaseTwo { get; private set; }
        public bool IsDead => isDead;
        public VolcanusPiece DamageablePiece => IsPhaseTwo ? Truso : Head;

        private Vector3 sawOriginLocalPosition;
        private Quaternion sawOriginLocalRotation;
        private float sawGroundOffset;
        private float fistGroundOffset;
        private bool isGroundOffsetCached;
        private bool isDead;

        protected override void Awake()
        {
            base.Awake();
            originPos = transform.position;
            if (healthModule == null) healthModule = GetModule<HealthModule>();
            if (deathModule == null) deathModule = GetModule<DeathModule>();
            if (deathModule != null) deathModule.OnDeath += HandleHealthDeath;
            BodyRoot = Truso != null ? Truso.transform.parent : transform;
            BodyOriginLocalPosition = BodyRoot.localPosition;
            if (feedback == null) feedback = GetComponent<VolcanusFeedback>();
            if (Saw == null && LeftHand != null)
                Saw = LeftHand.transform;
            if (Saw != null)
            {
                sawOriginLocalPosition = Saw.localPosition;
                sawOriginLocalRotation = Saw.localRotation;
            }
        }

        private void Start()
        {
            LeftHand?.SetOriginRotation(SawDownAngle);
            RightHand?.SetOriginRotation(FistDownAngle);
            CacheGroundOffsets();
            FindTarget();
            InitializeAttacks();
            if (playOnStart) StartCoroutine(AttackLoop());
        }

        private void InitializeAttacks()
        {
            foreach (VolcanusSkill attack in GetAttacks())
                attack?.InitializeAttack(this);
        }

        private VolcanusSkill[] GetAttacks()
        {
            return new VolcanusSkill[]
            {
                rightPunchAttack, sawSlamAttack, leftPunchAttack,
                sideSlashAttack, fiveSlamAttack, laserAttack, missileAttack
            };
        }

        private IEnumerator AttackLoop()
        {
            yield return new WaitUntil(() => FindTarget());
            yield return new WaitForSeconds(attackInterval);

            while (!isDead)
            {
                if (IsPhaseTwo) yield return PhaseTwoLoop();
                else yield return PhaseOneLoop();
            }
        }

        private IEnumerator PhaseOneLoop()
        {
            yield return PlayAttack(rightPunchAttack);
            yield return AttackWait();
            yield return PlayAttack(sawSlamAttack);
            yield return AttackWait();
            yield return PlayAttack(leftPunchAttack);
            yield return AttackWait();
            yield return PlayAttack(sideSlashAttack);
            yield return AttackWait();
            yield return PlayAttack(fiveSlamAttack);
            yield return AttackWait();
            yield return PlayAttack(laserAttack);
            yield return AttackWait();
        }

        private IEnumerator PhaseTwoLoop()
        {
            yield return RunParallel(sawSlamAttack, 1f, fiveSlamAttack, 1.5f);
            yield return AttackWait();
            yield return PlayAttack(missileAttack);
            yield return AttackWait();
            yield return RunParallel(rightPunchAttack, 1f, sawSlamAttack, 1f);
            yield return AttackWait();
            yield return PlayAttack(sideSlashAttack);
            yield return AttackWait();
        }

        private IEnumerator PlayAttack(VolcanusSkill attack, float durationScale = 1f)
        {
            if (attack != null) yield return attack.Play(durationScale);
        }

        private IEnumerator RunParallel(VolcanusSkill first, float firstScale, VolcanusSkill second, float secondScale)
        {
            int runningCount = 2;
            StartCoroutine(RunAttack(first, firstScale, () => runningCount--));
            StartCoroutine(RunAttack(second, secondScale, () => runningCount--));
            yield return new WaitUntil(() => runningCount <= 0 || isDead);
        }

        private IEnumerator RunAttack(VolcanusSkill attack, float durationScale, System.Action onComplete)
        {
            yield return PlayAttack(attack, durationScale);
            onComplete?.Invoke();
        }

        private WaitForSeconds AttackWait() => new WaitForSeconds(attackInterval);

        private bool FindTarget()
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
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, startY), Vector2.down, groundRayDistance, groundLayer);
            return hit.collider != null ? (Vector3)hit.point : new Vector3(x, ArenaCenter.y, 0f);
        }

        public Vector3 GetImpactVisualPosition(Transform piece, Vector3 groundPoint, Vector2 visualOffset)
        {
            Vector3 impactPosition = groundPoint + (Vector3)visualOffset;
            if (piece == null) return impactPosition;
            if (!isGroundOffsetCached) CacheGroundOffsets();
            if (piece == RightHand?.transform)
                impactPosition.y += fistGroundOffset;
            else if (piece == Saw)
                impactPosition.y += sawGroundOffset;
            else
                impactPosition.y += GetGroundOffset(piece);
            impactPosition.z = piece.position.z;
            return impactPosition;
        }

        public Vector3 GetSawImpactPosition(Vector3 groundPoint)
        {
            return GetImpactVisualPosition(Saw, groundPoint, sawImpactVisualOffset);
        }

        private void CacheGroundOffsets()
        {
            sawGroundOffset = GetGroundOffset(Saw);
            fistGroundOffset = GetGroundOffset(RightHand != null ? RightHand.transform : null);
            isGroundOffsetCached = true;
        }

        private float GetGroundOffset(Transform piece)
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

        public void ReactPieces(VolcanusPiece movingPiece, Vector2 direction)
        {
            Head?.React(movingPiece, direction * 0.8f + Vector2.up * 0.12f, -direction.x * 13f);
            Truso?.React(movingPiece, direction * 0.48f, -direction.x * 7f);
            LeftHand?.React(movingPiece, direction * 0.5f, -direction.x * 11f);
            RightHand?.React(movingPiece, direction * 0.5f, -direction.x * 11f);
        }

        public void SetSawMoving(bool isMoving)
        {
            if (Saw == null) return;
            Saw.DOKill();
            if (isMoving)
            {
                LeftHand?.SetAnotherMoving(true);
                Saw.localRotation = Quaternion.Euler(0f, 0f, SawDownAngle);
                return;
            }

            if (LeftHand != null && Saw == LeftHand.transform)
            {
                if (LeftHand.IsAnotherMoving) LeftHand.SetAnotherMoving(false);
                return;
            }

            Sequence sequence = DOTween.Sequence();
            sequence.Append(Saw.DOLocalMove(sawOriginLocalPosition, 0.4f).SetEase(Ease.InOutSine));
            sequence.Join(Saw.DOLocalRotateQuaternion(sawOriginLocalRotation, 0.4f).SetEase(Ease.InOutSine));
        }

        public void AttackReady(Vector3 position)
        {
            onAttackReady?.Invoke(position);
            feedback?.PlayReady();
        }

        public void AttackImpact(Vector3 position)
        {
            onAttackImpact?.Invoke(position);
            feedback?.PlayImpact();
        }

        public void MissileSpawn(Vector3 position)
        {
            onMissileSpawn?.Invoke(position);
            feedback?.PlayMissile();
        }

        public void LaserFeedback(bool followUp)
        {
            feedback?.PlayLaser();
        }
        public void SpawnFistRocks(Vector3 position) => SpawnRocks(position, fistRockCount);
        public void SpawnSawRocks(Vector3 position) => SpawnRocks(position, sawRockCount);

        private void SpawnRocks(Vector3 groundPoint, int count)
        {
            if (rockVisualPrefabs == null || rockVisualPrefabs.Length == 0 || count <= 0) return;

            for (int i = 0; i < count; i++)
            {
                GameObject visualPrefab = rockVisualPrefabs[Random.Range(0, rockVisualPrefabs.Length)];
                if (visualPrefab == null) continue;

                float centerRate = count <= 1 ? 0f : (float)i / (count - 1) - 0.5f;
                Vector3 spawnPosition = groundPoint + Vector3.up * rockSpawnHeight;
                GameObject rockObject = Instantiate(visualPrefab, spawnPosition, Quaternion.Euler(0f, 0f, Random.Range(-25f, 25f)));
                rockObject.name = "Volcanus Rock";
                rockObject.layer = LayerMask.NameToLayer("Prop");
                rockObject.transform.localScale *= rockScale;

                Rigidbody2D rockRigidbody = rockObject.AddComponent<Rigidbody2D>();
                rockRigidbody.mass = 0.7f;
                rockRigidbody.gravityScale = 2f;
                rockRigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                CircleCollider2D rockCollider = rockObject.AddComponent<CircleCollider2D>();
                SpriteRenderer rockRenderer = rockObject.GetComponent<SpriteRenderer>();
                if (rockRenderer != null && rockRenderer.sprite != null)
                {
                    rockCollider.radius = Mathf.Max(rockRenderer.sprite.bounds.extents.x, rockRenderer.sprite.bounds.extents.y) * 0.75f;
                    rockRenderer.sortingOrder = 15;
                }

                VolcanusRock rock = rockObject.AddComponent<VolcanusRock>();
                Vector2 launchDirection = new Vector2(centerRate * rockSpawnSpread + Random.Range(-0.25f, 0.25f), 1f).normalized;
                rock.Setting(
                    launchDirection * Random.Range(rockLaunchForce * 0.75f, rockLaunchForce * 1.25f),
                    Random.Range(-240f, 240f), rockDamage, rockLifeTime
                );
                rock.OnBreak += position => onRockBreak?.Invoke(position);
                onRockSpawn?.Invoke(spawnPosition);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos) return;

            Vector3 center = ArenaPos != null ? ArenaPos.position : transform.position;
            Gizmos.color = new Color(0.1f, 0.9f, 1f, 0.8f);
            Gizmos.DrawLine(center + Vector3.left * ArenaHalfWidth, center + Vector3.right * ArenaHalfWidth);
            Gizmos.DrawWireCube(center, new Vector3(ArenaHalfWidth * 2f, 0.2f, 0f));

            Gizmos.color = new Color(1f, 0.9f, 0.1f, 0.75f);
            for (int i = 0; i < 5; i++)
            {
                float rate = i / 4f;
                float x = Mathf.Lerp(center.x - ArenaHalfWidth * 0.75f, center.x + ArenaHalfWidth * 0.75f, rate);
                float startY = center.y + groundRayHeight;
                RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, startY), Vector2.down, groundRayDistance, groundLayer);
                Vector3 end = hit.collider != null ? (Vector3)hit.point : new Vector3(x, startY - groundRayDistance, 0f);
                Gizmos.DrawLine(new Vector3(x, startY, 0f), end);
                Gizmos.DrawWireSphere(end, 0.22f);
            }

            DrawPieceGizmo(Head, !Application.isPlaying || !IsPhaseTwo ? Color.magenta : Color.gray);
            DrawPieceGizmo(Truso, Application.isPlaying && IsPhaseTwo ? Color.magenta : Color.gray);
            DrawPieceGizmo(LeftHand, Color.gray);
            DrawPieceGizmo(RightHand, Color.gray);

            if (target != null)
            {
                Vector3 groundPoint = GetGroundPoint(target.position.x);
                Gizmos.color = new Color(1f, 0.45f, 0f, 0.85f);
                Gizmos.DrawWireSphere(groundPoint, rockSpawnSpread);
                Gizmos.DrawLine(groundPoint, groundPoint + Vector3.up * rockSpawnHeight);
            }
        }

        private void DrawPieceGizmo(VolcanusPiece piece, Color color)
        {
            if (piece == null) return;
            Gizmos.color = color;
            Collider2D pieceCollider = piece.GetComponentInChildren<Collider2D>();
            if (pieceCollider != null)
                Gizmos.DrawWireCube(pieceCollider.bounds.center, pieceCollider.bounds.size);
            else
                Gizmos.DrawWireSphere(piece.transform.position, 0.5f);
        }

        public void TakeDamage(VolcanusPiece hitPiece, DamageData damage)
        {
            if (hitPiece == null || hitPiece != DamageablePiece || hitPiece.IsDestroyed)
            {
                if (hitPiece != null)
                {
                    onBlockedHit?.Invoke(hitPiece.transform.position);
                }
                return;
            }

            onWeakPointHit?.Invoke(hitPiece.transform.position);
            feedback?.PlayDamage();
            hitPiece.React(null, Vector2.up * 0.22f, hitPiece == Head ? 5f : 8f);
            Truso?.React(hitPiece, Vector2.down * 0.16f, 0f);
            ApplyBossDamage(damage);
        }

        private void ApplyBossDamage(DamageData damage)
        {
            if (isDead || invincible || healthModule == null) return;
            healthModule.ApplyDamage(damage);
        }

        private void HandleHealthDeath()
        {
            if (isDead) return;
            if (IsPhaseTwo)
            {
                Death();
                return;
            }

            EnterPhaseTwo();
            if (deathModule != null) deathModule.Revive();
            else if (healthModule != null) healthModule.SetMaxHealth(healthModule.MaxHealth, true);
        }

        [ContextMenu("Enter Phase Two")]
        public void EnterPhaseTwo()
        {
            if (IsPhaseTwo || isDead) return;
            IsPhaseTwo = true;
            StopAllCoroutines();
            CancelAttacks();
            ReturnAllPieces();

            if (Head != null)
            {
                Head.SetAnotherMoving(true);
                Head.transform.SetParent(transform, true);
                Head.PieceDestroy();
                Head.Falling();
            }

            onPhaseTwo?.Invoke();
            feedback?.PlayPhase();
            StartCoroutine(PhaseTwoRestart());
        }

        private IEnumerator PhaseTwoRestart()
        {
            yield return new WaitForSeconds(1.1f);
            while (!isDead && IsPhaseTwo)
                yield return PhaseTwoLoop();
        }

        private void CancelAttacks()
        {
            foreach (VolcanusSkill attack in GetAttacks())
                attack?.StopSkill();
        }

        private void ReturnAllPieces()
        {
            if (RightHand != null && RightHand.IsAnotherMoving) RightHand.SetAnotherMoving(false);
            if (LeftHand != null && LeftHand.IsAnotherMoving) LeftHand.SetAnotherMoving(false);
            if (Truso != null && Truso.IsAnotherMoving) Truso.SetAnotherMoving(false);
            if (!IsPhaseTwo && Head != null && Head.IsAnotherMoving) Head.SetAnotherMoving(false);
            else if (IsPhaseTwo && Head != null) Head.transform.DOKill();
            SetSawMoving(false);
            BodyRoot.DOKill();
            BodyRoot.DOLocalMove(BodyOriginLocalPosition, 0.4f).SetEase(Ease.InOutSine);
            BodyRoot.DOLocalRotate(Vector3.zero, 0.4f).SetEase(Ease.InOutSine);
        }

        private void Death()
        {
            if (isDead) return;
            isDead = true;
            StopAllCoroutines();
            CancelAttacks();
            transform.DOKill();
            ReturnAllPieces();
            Truso?.PieceDestroy();
            onDeath?.Invoke();
        }

        private void OnDestroy()
        {
            if (deathModule != null) deathModule.OnDeath -= HandleHealthDeath;
            CancelAttacks();
            transform.DOKill();
            if (BodyRoot != null) BodyRoot.DOKill();
        }
    }

    [System.Serializable]
    public class VolcanusPositionEvent : UnityEvent<Vector3>
    {
    }
}
