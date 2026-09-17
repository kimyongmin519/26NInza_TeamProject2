using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Bosses;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public class Volcanus : PhasedBossController
    {
        [Header("Piece")]
        [field: SerializeField] public VolcanusPiece Head { get; private set; }
        [field: SerializeField] public VolcanusPiece Truso { get; private set; }
        [field: SerializeField] public VolcanusPiece LeftHand { get; private set; }
        [field: SerializeField] public VolcanusPiece RightHand { get; private set; }
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

        private Vector3 sawOriginLocalPosition;
        private Quaternion sawOriginLocalRotation;
        private float sawGroundOffset;
        private float fistGroundOffset;
        private bool isGroundOffsetCached;

        protected override void Awake()
        {
            base.Awake();
            BodyRoot = Truso != null ? Truso.transform.parent : transform;
            BodyOriginLocalPosition = BodyRoot.localPosition;
            if (feedback == null) feedback = GetComponent<VolcanusFeedback>();
            if (Saw == null && LeftHand != null) Saw = LeftHand.transform;

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
            CacheGroundOffsets();
            base.Start();
        }

        protected override IEnumerable<BossSkill> GetAttacks()
        {
            yield return rightPunchAttack;
            yield return sawSlamAttack;
            yield return leftPunchAttack;
            yield return sideSlashAttack;
            yield return fiveSlamAttack;
            yield return laserAttack;
            yield return missileAttack;
        }

        protected override IEnumerator PhaseOneLoop()
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

        protected override IEnumerator PhaseTwoLoop()
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

        public new Vector3 GetImpactVisualPosition(
            Transform piece,
            Vector3 groundPoint,
            Vector2 visualOffset)
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
            if (rock == null) rock = rockObject.AddComponent<VolcanusRock>();
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
