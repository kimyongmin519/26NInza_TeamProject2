using GGMLib.ObjectPool.Runtime;
using KimLIb.EventSystem;
using Member.KYM.Scripts.CoreSystems.Events;
using Member.KYM.Scripts.EffectSystems;
using UnityEngine;

namespace Member.KYM.Scripts.CoreSystems.Managers
{
    public class CreateManager : MonoBehaviour
    {
        [SerializeField] private EventChannelSO createChannel;
        [SerializeField] private PoolManagerSO poolManager;

        private void Awake()
        {
            createChannel.AddListener<ShowPoolingEffect>(HandleShowPoolingEffect);
        }

        private void OnDestroy()
        {
            createChannel.RemoveListener<ShowPoolingEffect>(HandleShowPoolingEffect);
        }

        private void HandleShowPoolingEffect(ShowPoolingEffect evt)
        {
            PoolableVfx vfx = poolManager.Pop<PoolableVfx>(evt.ItemData);
            if (vfx == null)
                return;

            vfx.OnVfxEnd += HandleVfxEnd;
            vfx.PlayVfx(evt.Context);
        }

        private void HandleVfxEnd(PoolableVfx vfx)
        {
            vfx.OnVfxEnd -= HandleVfxEnd;
            poolManager.Push(vfx);
        }
    }
}
