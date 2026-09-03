using GGMLib.ObjectPool.Runtime;
using KimLIb.ObjectPool.Runtime;
using UnityEngine;

public class WarningZone : MonoBehaviour, IPoolable
{
    [SerializeField] private PoolManagerSO poolManager;
    [field : SerializeField] public PoolItemSO PoolItem { get; set; }

    public GameObject GameObject => gameObject;

    private float currnetremmingTime;
    private float lifeTime;

    private void Update()
    {
        if (currnetremmingTime < lifeTime)
        {
            currnetremmingTime += Time.deltaTime;
        }
        else
        {
            poolManager.Push(this);
        }
    }

    public void ResetItem()
    {
        currnetremmingTime = 0f;
        lifeTime = 10f;
    }

    public void Setting(Vector2 size, Vector2 pos, float lifeTime)
    {
        transform.localScale = size;
        transform.position = pos;
        this.lifeTime = lifeTime;
    }


}
