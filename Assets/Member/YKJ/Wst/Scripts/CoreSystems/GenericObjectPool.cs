using System.Collections.Generic;
using UnityEngine;

namespace Member.Wst.Scripts.CoreSystems
{
    public class GenericObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Stack<T> _pool = new();
        private readonly List<T> _active = new();

        public GenericObjectPool(T prefab, Transform parent, int initCount = 0)
        {
            _prefab = prefab;
            _parent = parent;
            for (int i = 0; i < initCount; i++)
                _pool.Push(Create());
        }

        private T Create()
        {
            T instance = Object.Instantiate(_prefab, _parent);
            instance.gameObject.SetActive(false);
            return instance;
        }

        public T Get()
        {
            T instance = _pool.Count > 0 ? _pool.Pop() : Create();
            instance.gameObject.SetActive(true);
            _active.Add(instance);
            return instance;
        }

        public void Return(T instance)
        {
            instance.gameObject.SetActive(false);
            _active.Remove(instance);
            _pool.Push(instance);
        }
        public void Clear()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
                Return(_active[i]);
        }
    }
}