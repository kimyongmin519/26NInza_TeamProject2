using System;
using System.Collections;
using System.Collections.Generic;
using GGMLib.ObjectPool.Runtime;
using UnityEngine;

namespace Member.KYM.Scripts.EffectSystems
{
    public class PoolableVfx : AbstractMonoPoolable
    {
        [SerializeField] private GameObject effectObject;

        public event Action<PoolableVfx> OnVfxEnd;

        private IPlayableVfx _playableVfx;
        private IVfxContextReceiver[] _contextReceivers;
        private Coroutine _playRoutine;

        private void Awake()
        {
            CachePlayableVfx();
            CacheContextReceivers();
        }

        public override void ResetItem()
        {
            StopPlayRoutine();
            _playableVfx?.StopVfx();

            if (_contextReceivers != null)
            {
                foreach (IVfxContextReceiver receiver in _contextReceivers)
                    receiver.ResetContext();
            }

            OnVfxEnd = null;
        }

        public void PlayVfx(in VfxSpawnContext context)
        {
            if (_playableVfx == null)
                CachePlayableVfx();

            if (_playableVfx == null)
                return;

            if (_contextReceivers == null)
                CacheContextReceivers();

            transform.SetPositionAndRotation(
                context.Position,
                context.Rotation);

            foreach (IVfxContextReceiver receiver in _contextReceivers)
                receiver.ApplyContext(context);

            StopPlayRoutine();
            _playRoutine = StartCoroutine(PlayVfxRoutine());
        }

        public void PlayVfx(Vector3 position, Quaternion rotation)
        {
            VfxSpawnContext context =
                VfxSpawnContext.Default(position, rotation);
            PlayVfx(context);
        }

        private IEnumerator PlayVfxRoutine()
        {
            _playableVfx.PlayVfx();
            yield return new WaitForSeconds(_playableVfx.Duration);

            _playRoutine = null;
            OnVfxEnd?.Invoke(this);
        }

        private void CachePlayableVfx()
        {
            _playableVfx = effectObject != null
                ? effectObject.GetComponent<IPlayableVfx>()
                : null;
        }

        private void CacheContextReceivers()
        {
            MonoBehaviour[] behaviours =
                GetComponentsInChildren<MonoBehaviour>(true);
            List<IVfxContextReceiver> receivers = new();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IVfxContextReceiver receiver)
                    receivers.Add(receiver);
            }

            _contextReceivers = receivers.ToArray();
        }

        private void StopPlayRoutine()
        {
            if (_playRoutine == null)
                return;

            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        private void OnValidate()
        {
            if (effectObject != null &&
                effectObject.GetComponent<IPlayableVfx>() == null)
            {
                effectObject = null;
            }
        }
    }
}
