using UnityEngine;

namespace GGMLib.ObjectPool.Runtime
{
    [CreateAssetMenu(fileName = "Pool Item", menuName = "Lib/Pool/Item", order = 0)]
    public class PoolItemSO : ScriptableObject
    {
        public string itemName;
        public GameObject prefab;
        public int initCount;
    }
}
