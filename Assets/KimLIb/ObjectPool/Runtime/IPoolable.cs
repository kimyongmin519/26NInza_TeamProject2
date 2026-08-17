using GGMLib.ObjectPool.Runtime;
using UnityEngine;

namespace KimLIb.ObjectPool.Runtime
{
    public interface IPoolable
    {
        PoolItemSO PoolItem { get; set; }
        GameObject GameObject { get;}
        void ResetItem();
    }
}