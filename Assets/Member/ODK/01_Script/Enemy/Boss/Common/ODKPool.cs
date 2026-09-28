using System.Collections.Generic;
using GGMLib.ObjectPool.Runtime;
using KimLIb.ObjectPool.Runtime;
using Member.KYM.Scripts.EffectSystems;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Bosses
{
    public static class ODKPool
    {
        private static PoolManagerSO manager;
        private static Transform root;
        private static readonly Dictionary<GameObject, PoolItemSO> prefabMap = new();
        private static readonly Dictionary<string, PoolItemSO> nameMap = new();
        private static readonly HashSet<PoolItemSO> registered = new();

        public static bool IsReady => Resolve();

        private static bool Resolve()
        {
            if (manager != null && root != null) return true;

            manager = null;
            root = null;
            prefabMap.Clear();
            nameMap.Clear();
            registered.Clear();

            PoolInitializer[] initializers = Object.FindObjectsByType<PoolInitializer>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );
            PoolInitializer chosen = null;
            foreach (PoolInitializer initializer in initializers)
            {
                if (initializer == null || initializer.PoolManagerAsset == null) continue;
                if (chosen == null) chosen = initializer;
                if (initializer.gameObject.name.StartsWith("ODK"))
                {
                    chosen = initializer;
                    break;
                }
            }

            if (chosen == null) return false;

            manager = chosen.PoolManagerAsset;
            root = chosen.transform;
            foreach (PoolItemSO item in manager.itemList)
            {
                if (item == null) continue;
                registered.Add(item);
                if (item.prefab != null && !prefabMap.ContainsKey(item.prefab))
                    prefabMap.Add(item.prefab, item);
                if (!string.IsNullOrEmpty(item.itemName) && !nameMap.ContainsKey(item.itemName))
                    nameMap.Add(item.itemName, item);
            }
            return true;
        }

        public static PoolItemSO FindItem(string itemName)
        {
            if (string.IsNullOrEmpty(itemName) || !Resolve()) return null;
            return nameMap.TryGetValue(itemName, out PoolItemSO item) ? item : null;
        }

        public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null)
            where T : Component
        {
            if (prefab == null) return null;
            GameObject spawned = Spawn(prefab.gameObject, position, rotation, parent);
            return spawned != null ? spawned.GetComponent<T>() : null;
        }

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;

            if (Resolve() && prefabMap.TryGetValue(prefab, out PoolItemSO item))
            {
                IPoolable poolable = manager.Pop<IPoolable>(item);
                if (poolable != null && poolable.GameObject != null)
                {
                    poolable.PoolItem = item;
                    Transform spawned = poolable.GameObject.transform;
                    spawned.SetParent(parent != null ? parent : root, false);
                    spawned.SetPositionAndRotation(position, rotation);
                    spawned.localScale = prefab.transform.localScale;
                    if (poolable.GameObject.TryGetComponent(out Rigidbody2D body))
                    {
                        body.position = position;
                        body.rotation = rotation.eulerAngles.z;
                        body.linearVelocity = Vector2.zero;
                        body.angularVelocity = 0f;
                    }
                    return poolable.GameObject;
                }
            }

            return parent != null
                ? Object.Instantiate(prefab, position, rotation, parent)
                : Object.Instantiate(prefab, position, rotation);
        }

        public static void Despawn(Component component)
        {
            if (component == null) return;
            Despawn(component.gameObject);
        }

        public static void Despawn(GameObject target)
        {
            if (target == null) return;

            if (target.TryGetComponent(out IPoolable poolable) &&
                poolable.PoolItem != null &&
                Resolve() &&
                registered.Contains(poolable.PoolItem))
            {
                if (!target.activeSelf) return;
                target.transform.SetParent(root, false);
                manager.Push(poolable);
                return;
            }

            Object.Destroy(target);
        }

        public static bool PlayEffect(PoolItemSO item, Vector3 position, Quaternion rotation, Color tint)
        {
            if (item == null || !Resolve() || !registered.Contains(item)) return false;

            PoolableVfx vfx = manager.Pop<PoolableVfx>(item);
            if (vfx == null) return false;

            vfx.PoolItem = item;
            vfx.transform.SetParent(root, false);
            vfx.OnVfxEnd += HandleVfxEnd;
            vfx.PlayVfx(new VfxSpawnContext(position, rotation, tint));
            return true;
        }

        public static bool PlayEffect(string itemName, Vector3 position, Quaternion rotation, Color tint)
        {
            return PlayEffect(FindItem(itemName), position, rotation, tint);
        }

        private static void HandleVfxEnd(PoolableVfx vfx)
        {
            if (vfx == null) return;
            vfx.OnVfxEnd -= HandleVfxEnd;
            if (manager != null && vfx.gameObject.activeSelf) manager.Push(vfx);
        }
    }
}
