using DG.Tweening;
using GGMLib.ObjectPool.Runtime;
using KimLIb.ObjectPool.Runtime;
using Member.KYM.Scripts.CoreSystems;
using UnityEngine;

public class MoonBouncer : MonoBehaviour, IPoolable
{
    [SerializeField] private PlayerInputSO playerInputSO;
    [Header("Pool")]
    [SerializeField] private PoolManagerSO poolManagerSO;

    [field:SerializeField] public PoolItemSO PoolItem { get; set; }
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
    [SerializeField] private float jumpPower = 20f;
    [SerializeField] private float plusMoveStr = 3f;
    [SerializeField] private Ease ease = Ease.OutQuad;


    [Header("Ground Check Gizmo")]
    [SerializeField] private Vector2 downLayer = Vector2.one;
    [SerializeField] private Vector2 downLayerPlusPos;


    private Sequence sequence;
    private bool launched;


    public void ResetItem()
    {
        KillSequence();
        
        launched = false;
        targetPos = Vector2.zero;
        currentLifeTime = 0f;
    }


    public void Launch(Vector2 dir)
    {
        KillSequence();

        launched = true;

        if (target == null)
        {
            Debug.LogWarning($"{name}: MoonBouncer target이 없음.");
            return;
        }

        targetPos = target.position;

        // 첫 점프
        JumpToTarget(speed, ease);
    }


    private void Start()
    {
        // 풀에서 Launch()를 따로 호출하지 않는 경우를 위한 자동 시작.
        if (!launched && target != null)
        {
            Launch(Vector2.zero);
        }
    }


    private void Update()
    {
        if (!launched || target == null)
            return;
        if (currentLifeTime >= LifeTime)
        {
            poolManagerSO.Push(this);
            return;
        }
        else
        {
            currentLifeTime += Time.deltaTime;
        }
        if (playerInputSO.MoveDirX != 0f)
        {
            targetPosPlus.x = playerInputSO.MoveDirX * plusMoveStr;
        }
        else
        {
            targetPosPlus.x = 0f;

        }
        if (sequence == null || !sequence.IsActive() || sequence.IsComplete())
        {
            targetPos = target.position;

            JumpToTarget(jumpDuration, Ease.Linear);
        }
    }


    private void JumpToTarget(float duration, Ease jumpEase)
    {
        KillSequence();

        targetPos = target.position;

        sequence = DOTween.Sequence();

        sequence.Append(
            transform.DOJump(
                targetPos + targetPosPlus,
                jumpPower,
                1,
                duration
            )
            .SetEase(jumpEase)
        );
    }


    private void KillSequence()
    {
        if (sequence != null)
        {
            sequence.Kill();
            sequence = null;
        }

        // 혹시 transform에 따로 걸린 Tween이 있으면 정리
        transform.DOKill();
    }


    private void OnDisable()
    {
        KillSequence();
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