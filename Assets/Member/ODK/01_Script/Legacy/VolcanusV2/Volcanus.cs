using System.Collections;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Bosses;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus.Legacy
{
    public class Volcanus : PhasedBossController
    {
        public enum StrikePart
        {
            Fist,
            Saw,
            Head,
            Body
        }

        [Header("Piece")]
        [field: SerializeField] public VolcanusPiece Head { get; private set; }
        [field: SerializeField] public VolcanusPiece Truso { get; private set; }
        [field: SerializeField] public VolcanusPiece LeftHand { get; private set; }
        [field: SerializeField] public VolcanusPiece RightHand { get; private set; }
        [field: SerializeField] public Transform Saw { get; private set; }
        [field: SerializeField] public float SawDownAngle { get; private set; } = -42f;
        [field: SerializeField] public float FistDownAngle { get; private set; }

        [Header("Giant Scale")]
        [SerializeField, Min(0.1f)] private float bodyScale = 1.8f;
        [SerializeField, Min(0.1f)] private float headScale = 2.2f;
        [SerializeField, Range(0f, 0.8f)] private float headOverlap = 0.3f;
        [SerializeField] private bool pushHandsOutward = true;
        [SerializeField] private VolcanusOriginalVisualRig visualRig;

        [Header("Hitbox")]
        [SerializeField, Range(0.3f, 1.2f)] private float headHitboxRatio = 0.85f;
        [SerializeField, Range(0.3f, 1.2f)] private float bodyHitboxRatio = 0.8f;

        [Header("Phase Two Saw + Punch")]
        [SerializeField] private float punchStartSpeed = 1.3f;
        [SerializeField] private float punchSpeedStep = 0.45f;
        [SerializeField] private float punchMaxSpeed = 2.6f;
        [SerializeField, Min(1)] private int punchMaxCount = 4;

        [Header("Attack")]
        [SerializeField] private VolcanusFeedback feedback;

        [Header("Impact Position")]
        [SerializeField] private Vector2 sawImpactVisualOffset = new Vector2(0f, 0.1f);
        [field: SerializeField] public Vector2 FistImpactVisualOffset { get; private set; } =
            new Vector2(0f, -0.15f);

        [Header("Hit Feedback")]
        [SerializeField] private BossPositionEvent onWeakPointHit;
        [SerializeField] private BossPositionEvent onBlockedHit;

        public Transform BodyRoot { get; private set; }
        public Vector3 BodyOriginLocalPosition { get; private set; }
        public VolcanusPiece DamageablePiece => IsPhaseTwo ? Truso : Head;
        public bool UsesGolemVisual => visualRig != null && visualRig.IsActive;

        private Vector3 sawOriginLocalPosition;
        private Quaternion sawOriginLocalRotation;
        private float sawGroundOffset;
        private float fistGroundOffset;
        private bool isGroundOffsetCached;
        private Transform fistTip;
        private Transform sawTip;
        private bool giantApplied;

        private const int RightPunchIndex = 0;
        private const int LeftPunchIndex = 1;
        private const int SawSlamIndex = 2;
        private const int SideSlashIndex = 3;
        private const int FiveSlamIndex = 4;
        private const int LaserIndex = 5;

        protected override void Awake()
        {
            base.Awake();
            BodyRoot = Truso != null ? Truso.transform.parent : transform;
            BodyOriginLocalPosition = BodyRoot.localPosition;
            if (feedback == null) feedback = GetComponent<VolcanusFeedback>();
            if (visualRig == null) visualRig = GetComponent<VolcanusOriginalVisualRig>();
            if (Saw == null && LeftHand != null) Saw = LeftHand.transform;
            fistTip = RightHand != null ? FindChild(RightHand.transform, "Hand_0") : null;
            sawTip = LeftHand != null ? FindChild(LeftHand.transform, "Saw_0") : null;

            if (Saw != null)
            {
                sawOriginLocalPosition = Saw.localPosition;
                sawOriginLocalRotation = Saw.localRotation;
            }
        }

        protected override void Start()
        {
            LeftHand?.SetOriginRotation(SawDownAngle);
            RightHand?.SetOriginRotation(FistDownAngle);
            ApplyGiantScale();
            SyncHitboxes();
            CacheGroundOffsets();
            base.Start();
        }

        protected override IEnumerator PhaseOneLoop()
        {
            yield return PlayAttack(RightPunchIndex);
            yield return AttackWait();
            yield return PlayAttack(SawSlamIndex);
            yield return AttackWait();
            yield return PlayAttack(LeftPunchIndex);
            yield return AttackWait();
            yield return PlayAttack(SideSlashIndex);
            yield return AttackWait();
            yield return PlayAttack(FiveSlamIndex);
            yield return AttackWait();
            yield return PlayAttack(LaserIndex);
            yield return AttackWait();
        }

        protected override IEnumerator PhaseTwoLoop()
        {
            yield return RunParallel(SawSlamIndex, 1f, FiveSlamIndex, 1.5f);
            yield return AttackWait();
            yield return SawSlamWithAcceleratingPunch();
            yield return AttackWait();
            yield return PlayAttack(SideSlashIndex);
            yield return AttackWait();
            yield return PlayAttack(LaserIndex);
            yield return AttackWait();
        }

        private IEnumerator SawSlamWithAcceleratingPunch()
        {
            bool slamRunning = true;
            StartCoroutine(RunSawSlam(() => slamRunning = false));
            yield return null;

            float speed = punchStartSpeed;
            int count = 0;
            while (slamRunning && !IsDead && count < punchMaxCount)
            {
                yield return PlayAttack(RightPunchIndex, speed);
                speed = Mathf.Min(punchMaxSpeed, speed + punchSpeedStep);
                count++;
                yield return null;
            }

            while (slamRunning && !IsDead) yield return null;
        }

        private IEnumerator RunSawSlam(System.Action onComplete)
        {
            yield return PlayAttack(SawSlamIndex);
            onComplete?.Invoke();
        }

        private void ApplyGiantScale()
        {
            if (giantApplied) return;
            giantApplied = true;

            bool usesOriginalScale = Mathf.Approximately(bodyScale, 1f)
                && Mathf.Approximately(headScale, 1f)
                && !pushHandsOutward;
            if (!UsesGolemVisual && usesOriginalScale) return;

            if (UsesGolemVisual)
            {
                if (Head != null) Head.SetOriginScale(Head.transform.localScale * headScale);
                visualRig.ApplyGiantScale(bodyScale, headScale);
                visualRig.RebaseControlScale();
                return;
            }

            TryGetPartBounds(StrikePart.Body, out Bounds bodyBefore);
            if (Truso != null) Truso.SetOriginScale(Truso.transform.localScale * bodyScale);
            if (Head != null) Head.SetOriginScale(Head.transform.localScale * headScale);

            if (!TryGetPartBounds(StrikePart.Body, out Bounds body)) return;

            if (Head != null && TryGetPartBounds(StrikePart.Head, out Bounds head))
            {
                float desiredBottom = body.max.y - head.size.y * headOverlap;
                float deltaY = desiredBottom - head.min.y;
                Vector3 localDelta = Head.transform.parent != null
                    ? Head.transform.parent.InverseTransformVector(new Vector3(0f, deltaY, 0f))
                    : new Vector3(0f, deltaY, 0f);
                Head.SetOriginPosition(Head.OriginLocalPosition + localDelta);
            }

            if (!pushHandsOutward || bodyBefore.size == Vector3.zero) return;
            float growX = body.extents.x - bodyBefore.extents.x;
            MoveHandOrigin(RightHand, growX);
            MoveHandOrigin(LeftHand, -growX);
        }

        private static void MoveHandOrigin(VolcanusPiece hand, float worldDeltaX)
        {
            if (hand == null || Mathf.Approximately(worldDeltaX, 0f)) return;
            Transform parent = hand.transform.parent;
            Vector3 delta = parent != null
                ? parent.InverseTransformVector(new Vector3(worldDeltaX, 0f, 0f))
                : new Vector3(worldDeltaX, 0f, 0f);
            hand.SetOriginPosition(hand.OriginLocalPosition + delta);
        }

        private void SyncHitboxes()
        {
            if (Head != null && TryGetPartBounds(StrikePart.Head, out Bounds head))
            {
                BoxCollider2D headBox = Head.GetComponentInChildren<BoxCollider2D>(true);
                if (headBox == null) headBox = Head.gameObject.AddComponent<BoxCollider2D>();
                FitBox(headBox, head, headHitboxRatio);
            }

            if (Truso != null && TryGetPartBounds(StrikePart.Body, out Bounds body))
            {
                BoxCollider2D bodyBox = Truso.GetComponent<BoxCollider2D>();
                if (bodyBox == null)
                {
                    bodyBox = Truso.gameObject.AddComponent<BoxCollider2D>();
                    bodyBox.isTrigger = true;
                }
                FitBox(bodyBox, body, bodyHitboxRatio);
            }
        }

        private static void FitBox(BoxCollider2D box, Bounds bounds, float ratio)
        {
            Transform owner = box.transform;
            Vector3 lossy = owner.lossyScale;
            float sx = Mathf.Max(0.0001f, Mathf.Abs(lossy.x));
            float sy = Mathf.Max(0.0001f, Mathf.Abs(lossy.y));
            box.offset = owner.InverseTransformPoint(bounds.center);
            box.size = new Vector2(bounds.size.x / sx, bounds.size.y / sy) * ratio;
            box.enabled = true;
        }

        public bool TryGetPartBounds(StrikePart part, out Bounds bounds)
        {
            if (UsesGolemVisual && visualRig.TryGetBounds(part, out bounds)) return true;
            Transform target = part switch
            {
                StrikePart.Fist => fistTip != null ? fistTip : RightHand != null ? RightHand.transform : null,
                StrikePart.Saw => sawTip != null ? sawTip : Saw,
                StrikePart.Head => Head != null ? Head.transform : null,
                _ => Truso != null ? Truso.transform : null
            };
            if (TryGetRendererBounds(target, false, out bounds)) return true;
            return TryGetRendererBounds(target, true, out bounds);
        }

        public Vector3 GetStrikeCenter(StrikePart part)
        {
            if (TryGetPartBounds(part, out Bounds bounds)) return bounds.center;
            Transform fallback = part switch
            {
                StrikePart.Fist => RightHand != null ? RightHand.transform : transform,
                StrikePart.Saw => Saw != null ? Saw : transform,
                StrikePart.Head => Head != null ? Head.transform : transform,
                _ => Truso != null ? Truso.transform : transform
            };
            return fallback.position;
        }

        public float GetStrikeRadius(StrikePart part, float ratio = 0.6f)
        {
            if (!TryGetPartBounds(part, out Bounds bounds)) return 1.5f;
            return Mathf.Max(0.3f, Mathf.Max(bounds.extents.x, bounds.extents.y) * ratio);
        }

        public Vector3 GetGroundedPosition(Transform piece, StrikePart part, Vector3 groundPoint, float sinkRatio = 0.15f)
        {
            if (piece == null) return groundPoint;
            if (!TryGetPartBounds(part, out Bounds bounds))
                return new Vector3(groundPoint.x, groundPoint.y, piece.position.z);
            Vector3 contact = new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * sinkRatio, piece.position.z);
            Vector3 offset = piece.position - contact;
            Vector3 result = groundPoint + offset;
            result.z = piece.position.z;
            return result;
        }

        public Vector3 LaserOrigin
        {
            get
            {
                if (!IsPhaseTwo && Head != null && !Head.IsDestroyed) return GetStrikeCenter(StrikePart.Head);
                if (TryGetPartBounds(StrikePart.Body, out Bounds body))
                    return new Vector3(body.center.x, body.center.y + body.extents.y * 0.55f, body.center.z);
                return transform.position;
            }
        }

        public new Vector3 GetImpactVisualPosition(
            Transform piece,
            Vector3 groundPoint,
            Vector2 visualOffset)
        {
            if (piece == RightHand?.transform)
                return GetGroundedPosition(piece, StrikePart.Fist, groundPoint + (Vector3)visualOffset);
            if (piece == Saw)
                return GetGroundedPosition(piece, StrikePart.Saw, groundPoint + (Vector3)visualOffset);

            Vector3 impactPosition = groundPoint + (Vector3)visualOffset;
            if (piece == null) return impactPosition;
            if (!isGroundOffsetCached) CacheGroundOffsets();
            impactPosition.y += GetGroundOffset(piece);
            impactPosition.z = piece.position.z;
            return impactPosition;
        }

        public Vector3 GetSawImpactPosition(Vector3 groundPoint)
        {
            return GetGroundedPosition(Saw, StrikePart.Saw, groundPoint + (Vector3)sawImpactVisualOffset);
        }

        private void CacheGroundOffsets()
        {
            sawGroundOffset = GetGroundOffset(Saw);
            fistGroundOffset = GetGroundOffset(RightHand != null ? RightHand.transform : null);
            isGroundOffsetCached = true;
        }

        public static bool TryGetRendererBounds(Transform root, bool includeDisabled, out Bounds bounds)
        {
            bounds = default;
            if (root == null) return false;
            bool found = false;
            foreach (SpriteRenderer spriteRenderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (spriteRenderer == null || spriteRenderer.sprite == null) continue;
                if (!includeDisabled && (!spriteRenderer.enabled || !spriteRenderer.gameObject.activeInHierarchy))
                    continue;
                if (!found)
                {
                    bounds = spriteRenderer.bounds;
                    found = true;
                }
                else bounds.Encapsulate(spriteRenderer.bounds);
            }
            return found;
        }

        private static Transform FindChild(Transform root, string targetName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == targetName) return child;
            return null;
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
            sequence.Join(
                Saw.DOLocalRotateQuaternion(sawOriginLocalRotation, 0.4f)
                    .SetEase(Ease.InOutSine)
            );
        }

        public void LaserFeedback(bool followUp)
        {
            feedback?.PlayLaser();
        }

        protected override void OnAttackReady(Vector3 position)
        {
            feedback?.PlayReady();
        }

        protected override void OnAttackImpact(Vector3 position)
        {
            feedback?.PlayImpact();
        }

        protected override void OnMissileSpawn(Vector3 position)
        {
            feedback?.PlayMissile();
        }

        protected override void ConfigureRock(
            GameObject rockObject,
            Rigidbody2D rockRigidbody,
            Vector2 launchVelocity,
            float angularVelocity,
            float damage,
            float lifeTime)
        {
            VolcanusRock rock = rockObject.GetComponent<VolcanusRock>();
            if (rock == null)
            {
                Debug.LogError("Volcanus rock prefab is missing VolcanusRock.", rockObject);
                Destroy(rockObject);
                return;
            }
            rock.Setting(launchVelocity, angularVelocity, damage, lifeTime);
            rock.OnBreak += RockBreak;
        }

        public void TakeDamage(VolcanusPiece hitPiece, DamageData damage)
        {
            if (hitPiece == null || hitPiece != DamageablePiece || hitPiece.IsDestroyed)
            {
                if (hitPiece != null) onBlockedHit?.Invoke(hitPiece.transform.position);
                return;
            }

            onWeakPointHit?.Invoke(hitPiece.transform.position);
            feedback?.PlayDamage();
            hitPiece.React(null, Vector2.up * 0.22f, hitPiece == Head ? 5f : 8f);
            Truso?.React(hitPiece, Vector2.down * 0.16f, 0f);
            ApplyBossDamage(damage);
        }

        protected override void OnPhaseTwoEntered()
        {
            ReturnAllPieces();

            if (Head != null)
            {
                Head.SetAnotherMoving(true);
                Head.transform.SetParent(transform, true);
                Head.PieceDestroy();
                Head.Falling();
            }

            feedback?.PlayPhase();
        }

        protected override void OnBossDeath()
        {
            transform.DOKill();
            ReturnAllPieces();
            Truso?.PieceDestroy();
            feedback?.PlayDeath();
        }

        private void ReturnAllPieces()
        {
            if (RightHand != null && RightHand.IsAnotherMoving) RightHand.SetAnotherMoving(false);
            if (LeftHand != null && LeftHand.IsAnotherMoving) LeftHand.SetAnotherMoving(false);
            if (Truso != null && Truso.IsAnotherMoving) Truso.SetAnotherMoving(false);
            if (!IsPhaseTwo && Head != null && Head.IsAnotherMoving) Head.SetAnotherMoving(false);
            else if (IsPhaseTwo && Head != null) Head.transform.DOKill();

            SetSawMoving(false);
            if (BodyRoot == null) return;
            BodyRoot.DOKill();
            BodyRoot.DOLocalMove(BodyOriginLocalPosition, 0.4f).SetEase(Ease.InOutSine);
            BodyRoot.DOLocalRotate(Vector3.zero, 0.4f).SetEase(Ease.InOutSine);
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            DrawPieceGizmo(Head, !Application.isPlaying || !IsPhaseTwo ? Color.magenta : Color.gray);
            DrawPieceGizmo(Truso, Application.isPlaying && IsPhaseTwo ? Color.magenta : Color.gray);
            DrawPieceGizmo(LeftHand, Color.gray);
            DrawPieceGizmo(RightHand, Color.gray);
        }

        private static void DrawPieceGizmo(VolcanusPiece piece, Color color)
        {
            if (piece == null) return;

            Gizmos.color = color;
            Collider2D pieceCollider = piece.GetComponentInChildren<Collider2D>();
            if (pieceCollider != null)
                Gizmos.DrawWireCube(pieceCollider.bounds.center, pieceCollider.bounds.size);
            else
                Gizmos.DrawWireSphere(piece.transform.position, 0.5f);
        }

        protected override void OnDestroy()
        {
            transform.DOKill();
            if (BodyRoot != null) BodyRoot.DOKill();
            base.OnDestroy();
        }
    }
}
