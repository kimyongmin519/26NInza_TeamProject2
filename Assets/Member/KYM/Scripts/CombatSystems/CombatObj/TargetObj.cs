using System;
using KimLIb.AnimatorSystems;
using UnityEngine;
using UnityEngine.Events;

namespace Member.KYM.Scripts.CombatSystems.CombatObj
{
    [RequireComponent(typeof(Animator))]
    public class TargetObj : MonoBehaviour, IDamageable
    {
        [SerializeField] private int destroyHitCount;
        private float _currentHit;
        private Animator _animator;

        public UnityEvent OnDestroyObj;
        private bool _isDestroyed;

        [Header("파괴 딜레이")]
        [SerializeField] private float destroyDelay;
        
        public void TakeDamage(DamageData damage)
        {
            if (_isDestroyed) return;
            
            _currentHit++;
            if (destroyHitCount >= _currentHit)
            {
                _isDestroyed = true;
                OnDestroyObj?.Invoke(); 
                DestroyObj();
            }
        }

        private async void DestroyObj()
        {
            await Awaitable.WaitForSecondsAsync(destroyDelay);
            Destroy(gameObject);
        }

        #region 임시

        private void OnCollisionEnter2D(Collision2D other)
        {
            TakeDamage(new DamageData());
        }

        #endregion

        public void PlayClip(AnimParamSO param)
        {
            if (_animator == null)
                _animator = GetComponent<Animator>();
            
            _animator.Play(param.ParamHash);
        }
    }
}
