using System.Collections.Generic;
using KimLIb.ObjectPool.Runtime;
using UnityEngine;

namespace GGMLib.ObjectPool.Runtime
{
    public class Pool
    {
        private readonly Stack<IPoolable> _pool;
        private readonly Transform _parent;
        private readonly GameObject _prefab;

        public Pool(IPoolable poolable, Transform parent, int initCount)
        {
            _pool = new Stack<IPoolable>();
            _parent = parent;
            _prefab = poolable.GameObject;
            for (int i = 0; i < initCount; i++)
            {
                GameObject go = Object.Instantiate(_prefab, _parent);
                go.SetActive(false);
                IPoolable item = go.GetComponent<IPoolable>();
                Debug.Assert(item != null , $"Poolable 콤포넌트가 없습니다. {_prefab.name}");
                _pool.Push(item);
            }
        }

        public IPoolable Pop()
        {
            IPoolable item;
            if (_pool.Count == 0)
            {
                GameObject go = Object.Instantiate(_prefab, _parent);
                item = go.GetComponent<IPoolable>();
                Debug.Assert(item != null, $"Poolable 콤포넌트가 없습니다. {_prefab.name}");
            }
            else
            {
                item = _pool.Pop();
                item.GameObject.SetActive(true);
            }
            item.ResetItem();
            return item;
        }

        public void Push(IPoolable item)
        {
            item.GameObject.SetActive(false);
            _pool.Push(item);
        }
    }
}