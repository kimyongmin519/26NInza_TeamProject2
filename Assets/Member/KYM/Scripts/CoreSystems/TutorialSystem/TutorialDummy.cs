using DG.Tweening;
using Member.ODK._01_Script;
using UnityEngine;
using UnityEngine.Events;

namespace Member.KYM.Scripts.CoreSystems.TutorialSystem
{
    [DisallowMultipleComponent]
    public sealed class TutorialDummy : MonoBehaviour, IDamageable
    {
        [Header("피격 연출")]
        [SerializeField] private Transform visual;
        [SerializeField, Min(0f)] private float shakeDuration = 0.2f;
        [SerializeField] private Vector3 shakeStrength = new(0.07f, 0.02f, 0f);

        [Header("피격 이벤트")]
        [SerializeField] private UnityEvent onHit;

        public int HitCount { get; private set; }

        private Vector3 _restLocalPosition;
        private Tween _hitTween;

        private void Awake()
        {
            if (visual != null)
                _restLocalPosition = visual.localPosition;
        }

        public void TakeDamage(DamageData damage)
        {
            HitCount++;
            onHit?.Invoke();

            if (visual == null || shakeDuration <= 0f)
                return;

            _hitTween?.Kill();
            visual.localPosition = _restLocalPosition;
            _hitTween = visual.DOShakePosition(shakeDuration, shakeStrength)
                .OnComplete(() =>
                {
                    visual.localPosition = _restLocalPosition;
                    _hitTween = null;
                });
        }

        private void OnDisable()
        {
            _hitTween?.Kill();
            _hitTween = null;

            if (visual != null)
                visual.localPosition = _restLocalPosition;
        }

        private void OnValidate()
        {
            shakeDuration = Mathf.Max(0f, shakeDuration);
        }
    }
}
