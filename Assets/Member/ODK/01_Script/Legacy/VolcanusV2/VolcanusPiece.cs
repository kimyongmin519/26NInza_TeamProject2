using DG.Tweening;
using Member.ODK._01_Script;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus.Legacy
{
    public class VolcanusPiece : MonoBehaviour, IDamageable
    {
        public bool IsDestroyed { get; private set; }
        public bool IsAnotherMoving { get; private set; }
        public Animator Animator { get; private set; }
        public Vector3 OriginLocalPosition => originLocalPos;
        public Quaternion OriginLocalRotation => originLocalRot;

        [Header("Animation")]
        [SerializeField] private string defaultAnimationStateName = "IDLE";
        [SerializeField] private float defaultAnimationFadeDuration = 0.08f;
        [SerializeField] private string destroyAnimationStateName;
        [Header("Return")]
        [SerializeField] private float returnDuration = 0.4f;
        [SerializeField] private Ease returnEase = Ease.InOutSine;
        [Header("Reaction")]
        [SerializeField] private float reactionDuration = 0.16f;
        [SerializeField] private float reactionReturnDuration = 0.32f;
        [Header("Falling")]
        [SerializeField] private float fallingGravityScale = 0.6f;

        [Header("Idle Motion")]
        [SerializeField] private bool idleMotionEnabled = true;
        [SerializeField] private Vector2 idleMove = new Vector2(0f, 0.18f);
        [SerializeField] private float idleRotation = 2f;
        [SerializeField, Min(0.2f)] private float idleDuration = 1.6f;
        [SerializeField, Min(0f)] private float idlePhaseDelay;
        [SerializeField, Range(0f, 0.1f)] private float idleScalePulse = 0.015f;

        private Vector3 originLocalPos;
        private Quaternion originLocalRot;
        private Vector3 originLocalScale;
        private Rigidbody2D rb;
        private Volcanus owner;
        private VolcanusPixelAnimator pixelAnimator;
        private Sequence returnSequence;
        private Sequence reactionSequence;
        private Sequence idleSequence;
        private bool hasStarted;

        private void Awake()
        {
            originLocalPos = transform.localPosition;
            originLocalRot = transform.localRotation;
            originLocalScale = transform.localScale;
            Animator = GetComponentInChildren<Animator>();
            pixelAnimator = GetComponentInChildren<VolcanusPixelAnimator>();
            rb = GetComponentInChildren<Rigidbody2D>();
            owner = GetComponentInParent<Volcanus>();
        }

        private void Start()
        {
            hasStarted = true;
            StartIdleMotion();
        }

        [ContextMenu("Death")]
        public void PieceDestroy()
        {
            if (IsDestroyed) return;
            IsDestroyed = true;
            KillTween();
            pixelAnimator?.Stop();
            if (Animator != null && !string.IsNullOrWhiteSpace(destroyAnimationStateName))
                Animator.Play(destroyAnimationStateName, 0, 0f);
        }

        public void Falling()
        {
            KillTween();
            if (rb == null) return;

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = fallingGravityScale;
            rb.WakeUp();
        }

        public void SetAnotherMoving(bool isMoving)
        {
            IsAnotherMoving = isMoving;
            KillTween();
            if (isMoving)
            {
                RestoreIdlePose();
                return;
            }

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.gravityScale = 0f;
            }
            ReturnToOrigin();
        }

        public void React(VolcanusPiece movingPiece, Vector2 offset, float angle)
        {
            if (this == movingPiece || IsDestroyed || IsAnotherMoving) return;

            reactionSequence?.Kill();
            StopIdleMotion(false);
            transform.DOKill();
            transform.localScale = originLocalScale;
            reactionSequence = DOTween.Sequence();
            reactionSequence.Append(transform.DOLocalMove(originLocalPos - (Vector3)offset * 0.4f, reactionDuration * 0.45f).SetEase(Ease.InOutSine));
            reactionSequence.Join(transform.DOLocalRotateQuaternion(originLocalRot * Quaternion.Euler(0f, 0f, -angle * 0.35f), reactionDuration * 0.45f).SetEase(Ease.InOutSine));
            reactionSequence.Append(transform.DOLocalMove(originLocalPos + (Vector3)offset, reactionDuration).SetEase(Ease.OutExpo));
            reactionSequence.Join(transform.DOLocalRotateQuaternion(originLocalRot * Quaternion.Euler(0f, 0f, angle), reactionDuration).SetEase(Ease.OutExpo));
            reactionSequence.Append(transform.DOLocalMove(originLocalPos - (Vector3)offset * 0.18f, reactionReturnDuration * 0.35f).SetEase(Ease.InOutQuad));
            reactionSequence.Join(transform.DOLocalRotateQuaternion(originLocalRot * Quaternion.Euler(0f, 0f, -angle * 0.2f), reactionReturnDuration * 0.35f).SetEase(Ease.InOutQuad));
            reactionSequence.Append(transform.DOLocalMove(originLocalPos, reactionReturnDuration).SetEase(Ease.OutBack));
            reactionSequence.Join(transform.DOLocalRotateQuaternion(originLocalRot, reactionReturnDuration).SetEase(Ease.OutBack));
            reactionSequence.Join(transform.DOScale(originLocalScale, reactionReturnDuration).SetEase(Ease.OutBack));
            reactionSequence.OnComplete(() =>
            {
                reactionSequence = null;
                StartIdleMotion();
            });
        }

        public void TakeDamage(DamageData damage)
        {
            if (owner == null)
                owner = GetComponentInParent<Volcanus>();
            owner?.TakeDamage(this, damage);
        }

        public void SetOriginRotation(float angle)
        {
            StopIdleMotion(true);
            originLocalRot = Quaternion.Euler(0f, 0f, angle);
            if (!IsAnotherMoving)
                transform.localRotation = originLocalRot;
            if (hasStarted) StartIdleMotion();
        }

        public void SetOriginScale(Vector3 scale)
        {
            StopIdleMotion(false);
            originLocalScale = scale;
            if (!IsAnotherMoving) transform.localScale = scale;
            if (hasStarted) StartIdleMotion();
        }

        public void SetOriginPosition(Vector3 localPosition)
        {
            StopIdleMotion(false);
            originLocalPos = localPosition;
            if (!IsAnotherMoving) transform.localPosition = localPosition;
            if (hasStarted) StartIdleMotion();
        }

        private void ReturnToOrigin()
        {
            if (IsDestroyed) return;
            returnSequence = DOTween.Sequence();
            returnSequence.Append(transform.DOLocalMove(originLocalPos, returnDuration).SetEase(returnEase));
            returnSequence.Join(transform.DOLocalRotateQuaternion(originLocalRot, returnDuration).SetEase(returnEase));
            returnSequence.Join(transform.DOScale(originLocalScale, returnDuration).SetEase(returnEase));
            returnSequence.OnComplete(() =>
            {
                returnSequence = null;
                StartIdleMotion();
            });
        }

        private void StartIdleMotion()
        {
            if (!idleMotionEnabled || IsDestroyed || IsAnotherMoving || !isActiveAndEnabled)
                return;

            StopIdleMotion(false);
            Vector3 halfMove = new Vector3(idleMove.x, idleMove.y, 0f) * 0.5f;
            Vector3 upperScale = new Vector3(
                originLocalScale.x * (1f + idleScalePulse),
                originLocalScale.y * (1f - idleScalePulse),
                originLocalScale.z
            );

            transform.localPosition = originLocalPos - halfMove;
            transform.localRotation = originLocalRot * Quaternion.Euler(0f, 0f, -idleRotation);
            transform.localScale = originLocalScale;

            idleSequence = DOTween.Sequence();
            if (idlePhaseDelay > 0f) idleSequence.AppendInterval(idlePhaseDelay);
            idleSequence.Append(
                transform.DOLocalMove(originLocalPos + halfMove, idleDuration)
                    .SetEase(Ease.InOutSine)
            );
            idleSequence.Join(
                transform.DOLocalRotateQuaternion(
                    originLocalRot * Quaternion.Euler(0f, 0f, idleRotation),
                    idleDuration
                ).SetEase(Ease.InOutSine)
            );
            idleSequence.Join(transform.DOScale(upperScale, idleDuration).SetEase(Ease.InOutSine));
            idleSequence.SetLoops(-1, LoopType.Yoyo);
        }

        private void StopIdleMotion(bool restorePose)
        {
            idleSequence?.Kill();
            idleSequence = null;
            if (restorePose) RestoreIdlePose();
        }

        private void RestoreIdlePose()
        {
            transform.localPosition = originLocalPos;
            transform.localRotation = originLocalRot;
            transform.localScale = originLocalScale;
        }

        public bool PlayAttackAnimation(string stateName)
        {
            if (IsDestroyed) return false;
            if (pixelAnimator != null) return pixelAnimator.Play(stateName);
            if (Animator == null || Animator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(stateName))
                return false;

            int stateHash = UnityEngine.Animator.StringToHash(stateName);
            if (!Animator.HasState(0, stateHash)) return false;
            Animator.Play(stateHash, 0, 0f);
            return true;
        }

        public void PlayDefaultAnimation()
        {
            if (pixelAnimator != null)
            {
                pixelAnimator.PlayDefault();
                return;
            }
            if (IsDestroyed || Animator == null || Animator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(defaultAnimationStateName)) return;
            int stateHash = UnityEngine.Animator.StringToHash(defaultAnimationStateName);
            if (Animator.HasState(0, stateHash))
                Animator.CrossFadeInFixedTime(stateHash, defaultAnimationFadeDuration, 0, 0f);
        }

        public bool IsUseable() => !IsDestroyed && !IsAnotherMoving;

        private void KillTween()
        {
            returnSequence?.Kill();
            reactionSequence?.Kill();
            returnSequence = null;
            reactionSequence = null;
            StopIdleMotion(false);
            transform.DOKill();
        }

        private void OnDestroy() => KillTween();
    }
}
