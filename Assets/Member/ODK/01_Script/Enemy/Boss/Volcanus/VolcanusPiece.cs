using DG.Tweening;
using UnityEngine;

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

    private Vector3 originLocalPos;
    private Quaternion originLocalRot;
    private Rigidbody2D rb;
    private Volcanus owner;
    private Sequence returnSequence;
    private Sequence reactionSequence;

    private void Awake()
    {
        originLocalPos = transform.localPosition;
        originLocalRot = transform.localRotation;
        Animator = GetComponentInChildren<Animator>();
        rb = GetComponentInChildren<Rigidbody2D>();
        owner = GetComponentInParent<Volcanus>();
    }

    [ContextMenu("Death")]
    public void PieceDestroy()
    {
        if (IsDestroyed) return;
        IsDestroyed = true;
        KillTween();
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
        if (isMoving) return;

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
        transform.DOKill();
        reactionSequence = DOTween.Sequence();
        reactionSequence.Append(transform.DOLocalMove(originLocalPos - (Vector3)offset * 0.4f, reactionDuration * 0.45f).SetEase(Ease.InOutSine));
        reactionSequence.Join(transform.DOLocalRotateQuaternion(originLocalRot * Quaternion.Euler(0f, 0f, -angle * 0.35f), reactionDuration * 0.45f).SetEase(Ease.InOutSine));
        reactionSequence.Append(transform.DOLocalMove(originLocalPos + (Vector3)offset, reactionDuration).SetEase(Ease.OutExpo));
        reactionSequence.Join(transform.DOLocalRotateQuaternion(originLocalRot * Quaternion.Euler(0f, 0f, angle), reactionDuration).SetEase(Ease.OutExpo));
        reactionSequence.Append(transform.DOLocalMove(originLocalPos - (Vector3)offset * 0.18f, reactionReturnDuration * 0.35f).SetEase(Ease.InOutQuad));
        reactionSequence.Join(transform.DOLocalRotateQuaternion(originLocalRot * Quaternion.Euler(0f, 0f, -angle * 0.2f), reactionReturnDuration * 0.35f).SetEase(Ease.InOutQuad));
        reactionSequence.Append(transform.DOLocalMove(originLocalPos, reactionReturnDuration).SetEase(Ease.OutBack));
        reactionSequence.Join(transform.DOLocalRotateQuaternion(originLocalRot, reactionReturnDuration).SetEase(Ease.OutBack));
        reactionSequence.OnComplete(() => reactionSequence = null);
    }

    public void TakeDamage(DamageData damage)
    {
        if (owner == null)
            owner = GetComponentInParent<Volcanus>();
        owner?.TakeDamage(this, damage);
    }

    public void SetOriginRotation(float angle)
    {
        originLocalRot = Quaternion.Euler(0f, 0f, angle);
        if (!IsAnotherMoving)
            transform.localRotation = originLocalRot;
    }

    private void ReturnToOrigin()
    {
        if (IsDestroyed) return;
        returnSequence = DOTween.Sequence();
        returnSequence.Append(transform.DOLocalMove(originLocalPos, returnDuration).SetEase(returnEase));
        returnSequence.Join(transform.DOLocalRotateQuaternion(originLocalRot, returnDuration).SetEase(returnEase));
        returnSequence.OnComplete(() => returnSequence = null);
    }

    public void PlayDefaultAnimation()
    {
        if (IsDestroyed || Animator == null || string.IsNullOrWhiteSpace(defaultAnimationStateName)) return;
        Animator.CrossFadeInFixedTime(defaultAnimationStateName, defaultAnimationFadeDuration, 0, 0f);
    }

    public bool IsUseable() => !IsDestroyed && !IsAnotherMoving;

    private void KillTween()
    {
        returnSequence?.Kill();
        reactionSequence?.Kill();
        transform.DOKill();
    }

    private void OnDestroy() => KillTween();
}
