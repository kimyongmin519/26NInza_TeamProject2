using KimLIb.AnimatorSystems;
using Member.ODK._01_Script;
using UnityEngine;
using UnityEngine.Events;

namespace Member.KYM.Scripts.CoreSystems.TutorialSystem
{
    [DisallowMultipleComponent]
    public sealed class TutorialDummy : MonoBehaviour, IDamageable
    {
        [Header("피격 연출")]
        [SerializeField] private Animator animator;
        [SerializeField] private AnimParamSO hitParam;

        [Header("피격 이벤트")]
        [SerializeField] private UnityEvent onHit;

        public int HitCount { get; private set; }

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        public void TakeDamage(DamageData damage)
        {
            HitCount++;
            onHit?.Invoke();

            if (animator != null && hitParam != null)
                animator.Play(hitParam.ParamHash, 0, 0f);
        }
    }
}
