using System.Collections.Generic;
using GGMLib.ObjectPool.Runtime;
using Member.KYM.Scripts.EffectSystems;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    [DisallowMultipleComponent]
    public sealed class LostSoulVfxPool : MonoBehaviour
    {
        [SerializeField] private PoolManagerSO poolManager;
        [SerializeField] private string effectSortingLayer = "Vfx";
        [SerializeField] private int effectSortingOrder = 60;

        private readonly HashSet<PoolableVfx> activeEffects = new();
        private bool initialized;

        private void Awake() => EnsureInitialized();

        public bool Play(
            PoolItemSO item,
            Vector3 position,
            Quaternion rotation)
        {
            if (item == null || !EnsureInitialized())
                return false;

            PoolableVfx effect = poolManager.Pop<PoolableVfx>(item);
            if (effect == null)
                return false;

            effect.PoolItem = item;
            effect.transform.SetParent(transform, false);
            effect.OnVfxEnd -= HandleEffectEnd;
            effect.OnVfxEnd += HandleEffectEnd;
            activeEffects.Add(effect);
            ApplySorting(effect);
            effect.PlayVfx(new VfxSpawnContext(position, rotation, Color.white));
            return true;
        }

        private void ApplySorting(PoolableVfx effect)
        {
            bool validLayer = !string.IsNullOrEmpty(effectSortingLayer) &&
                              (SortingLayer.NameToID(effectSortingLayer) != 0 || effectSortingLayer == "Default");
            foreach (Renderer renderer in effect.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                if (validLayer) renderer.sortingLayerName = effectSortingLayer;
                renderer.sortingOrder = effectSortingOrder;
            }
        }

        private bool EnsureInitialized()
        {
            if (initialized)
                return poolManager != null;

            initialized = true;
            if (poolManager == null)
                return false;

            poolManager.InitializePool(transform);
            return true;
        }

        private void HandleEffectEnd(PoolableVfx effect)
        {
            if (effect == null)
                return;

            effect.OnVfxEnd -= HandleEffectEnd;
            activeEffects.Remove(effect);
            if (poolManager != null && effect.gameObject.activeSelf)
                poolManager.Push(effect);
        }

        private void OnDestroy()
        {
            foreach (PoolableVfx effect in activeEffects)
            {
                if (effect != null)
                    effect.OnVfxEnd -= HandleEffectEnd;
            }
            activeEffects.Clear();
        }
    }
}
