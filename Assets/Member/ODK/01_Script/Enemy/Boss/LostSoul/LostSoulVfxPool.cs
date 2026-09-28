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
            effect.PlayVfx(new VfxSpawnContext(position, rotation, Color.white));
            return true;
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
