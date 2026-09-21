using DG.Tweening;
using UnityEngine;

namespace Member.KYM.Scripts.CoreSystems.MapSystem
{
    public class MoveEaseLevel : MonoBehaviour, IMapDirectTarget
    {
        [Header("신호")]
        [field: SerializeField] public int SignalId { get; private set; }

        [Header("이동")]
        [SerializeField] private Vector2 moveOffset;
        [SerializeField, Min(0f)] private float duration = 1f;
        [SerializeField] private Ease ease = Ease.InOutSine;

        private Vector3 _targetLocalPosition;
        private Tween _moveTween;
        private bool _isInitialized;

        private void Awake()
        {
            InitializePosition();
        }

        public void PlayDirect()
        {
            if (!_isInitialized)
                InitializePosition();

            _moveTween?.Kill();

            if (duration <= 0f)
            {
                transform.localPosition = _targetLocalPosition;
                _moveTween = null;
                return;
            }

            _moveTween = transform
                .DOLocalMove(_targetLocalPosition, duration)
                .SetEase(ease)
                .OnComplete(() => _moveTween = null);
        }

        private void InitializePosition()
        {
            _targetLocalPosition = transform.localPosition + (Vector3)moveOffset;
            _isInitialized = true;
        }

        private void OnDestroy()
        {
            _moveTween?.Kill();
            _moveTween = null;
        }

        private void OnValidate()
        {
            duration = Mathf.Max(0f, duration);
        }
    }
}
