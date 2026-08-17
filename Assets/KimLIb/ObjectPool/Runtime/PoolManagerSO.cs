using System.Collections.Generic;
using KimLIb.ObjectPool.Runtime;
using UnityEngine;

namespace GGMLib.ObjectPool.Runtime
{
    [CreateAssetMenu(fileName = "PoolManager so", menuName = "Lib/Pool/PoolManager", order = 5)]
    public class PoolManagerSO : ScriptableObject
    {
        public List<PoolItemSO> itemList = new();

        private Dictionary<PoolItemSO, Pool> _poolDict;
        private Transform _rootTrm;

        public void InitializePool(Transform rootTrm)
        {
            _rootTrm = rootTrm;
            _poolDict = new Dictionary<PoolItemSO, Pool>();

            foreach (PoolItemSO poolItem in itemList)
            {
                IPoolable poolable = poolItem.prefab.GetComponent<IPoolable>();
                Debug.Assert(poolable != null, $"PoolItem은 반드시 IPoolable을 구현해야 합니다. {poolItem}");
                
                Pool pool = new Pool(poolable, _rootTrm, poolItem.initCount);
                _poolDict.Add(poolItem, pool);
            }
        }

        public T Pop<T>(PoolItemSO poolItem) where T : IPoolable
        {
            if (_rootTrm == null)
            {
                Debug.LogWarning("풀매니저 초기화가 안된상태입니다. 강제로 만듭니다.");
                GameObject go = new GameObject("PoolMono");
                InitializePool(go.transform);
            }

            if (_poolDict.TryGetValue(poolItem, out Pool pool))
            {
                return (T)pool.Pop();
            }
            return default;
        }

        public void Push(IPoolable item)
        {
            Debug.Assert(item != null, "아이템은 널일 수 없습니다.");
            if(_poolDict.TryGetValue(item.PoolItem, out Pool pool))
                pool.Push(item);
        }
    }
}