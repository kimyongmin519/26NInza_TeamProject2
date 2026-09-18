using DG.Tweening;
using GGMLib.ObjectPool.Runtime;
using KimLIb.ObjectPool.Runtime;
using Member.KYM.Scripts.CoreSystems;
using UnityEngine;

public class MoonBouncerVelocity : MonoBehaviour, IPoolable
{
    [SerializeField] private PlayerInputSO playerInputSO;

    [Header("Pool")]
    [SerializeField] private PoolManagerSO poolManagerSO;

    [field: SerializeField] public PoolItemSO PoolItem { get; set; }
    public GameObject GameObject => gameObject;


    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 targetPos;
    [SerializeField] private Vector2 targetPosPlus;


    [Header("Movement")]
    [SerializeField] private float speed = 1f;

    [SerializeField] private float LifeTime = 5f;
    private float currentLifeTime = 0f;
    [SerializeField] private float jumpDuration = 1f;

    [SerializeField] private float jumpPower = 3f;
    [SerializeField] private float plusMoveStr = 3f;

    [SerializeField] private Ease ease = Ease.OutQuad;


    [Header("Ground Check Gizmo")]
    [SerializeField] private Vector2 downLayer = Vector2.one;
    [SerializeField] private Vector2 downLayerPlusPos;


    private Tween jumpTween;

    private bool launched;


    public void ResetItem()
    {
        KillTweens();

        launched = false;

        targetPos = Vector2.zero;
        targetPosPlus = Vector2.zero;
        currentLifeTime = 0f;
    }


    public void Launch(Vector2 dir)
    {
        KillTweens();

        if (target == null)
        {
            Debug.LogWarning($"{name}: MoonBouncer target이 없음.");
            return;
        }

        launched = true;

        UpdateTargetOffset();

        

        // 첫 점프
        JumpToTarget(speed, ease);
    }


    private void Start()
    {
        if (!launched && target != null)
        {
            Launch(Vector2.zero);
        }
    }


    private void Update()
    {
        if (currentLifeTime < LifeTime)
        {
            currentLifeTime += Time.deltaTime;
        }
        else
        {
            ReturnToPool();
        }
        if (!launched || target == null)
            return;

        UpdateTargetOffset();
    }


    private void UpdateTargetOffset()
    {
        if (playerInputSO == null)
        {
            targetPosPlus.x = 0f;
            return;
        }

        if (playerInputSO.MoveDirX != 0f)
        {
            if (playerInputSO.MoveDirX * targetPosPlus.x < 0f)
            {
                targetPosPlus.x = 0f;
            }
            else
            {
                targetPosPlus.x +=
                    playerInputSO.MoveDirX *
                    plusMoveStr *
                    Time.deltaTime;
            }
        }
    }


    private void JumpToTarget(float duration, Ease jumpEase)
    {
        if (!launched || target == null)
            return;

        KillJumpTween();

        targetPos =
            (Vector2)target.position +
            targetPosPlus;

        jumpTween = transform
            .DOJump(
                targetPos,
                jumpPower,
                1,
                duration
            )
            .SetEase(jumpEase)
            .OnComplete(() =>
            {
                if (!launched || target == null)
                    return;

                JumpToTarget(jumpDuration, Ease.Linear);
            });
    }


    private void ReturnToPool()
    {
        if (!launched)
            return;

        launched = false;

        KillJumpTween();



        poolManagerSO.Push(this);
    }


    private void KillJumpTween()
    {
        if (jumpTween != null)
        {
            jumpTween.Kill();
            jumpTween = null;
        }

        transform.DOKill();
    }


    private void KillTweens()
    {
        KillJumpTween();


    }


    private void OnDisable()
    {
        launched = false;

        KillTweens();
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireCube(
            transform.position + (Vector3)downLayerPlusPos,
            downLayer
        );
    }
}