using DG.Tweening;
using UnityEngine;

namespace Member.KYM.Scripts.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UIShowFromTop : MonoBehaviour
    {
        [Header("등장 연출")]
        [SerializeField, Min(0f)] private float duration = 0.7f;
        [SerializeField] private Ease ease = Ease.OutCubic;
        [SerializeField, Min(0f)] private float hiddenOffsetY = 200f;

        private RectTransform _rectTransform;
        private Vector2 _shownPosition;
        private Tween _showTween;
        private bool _isShown;
        private bool _isInitialized;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            Initialize();
        }

        [ContextMenu("체력바 등장")]
        public void Show()
        {
            Initialize();

            if (_isShown)
                return;

            _isShown = true;
            _showTween?.Kill();

            if (duration <= 0f)
            {
                _rectTransform.anchoredPosition = _shownPosition;
                return;
            }

            _showTween = _rectTransform
                .DOAnchorPos(_shownPosition, duration)
                .SetEase(ease)
                .SetUpdate(true)
                .OnComplete(() => _showTween = null);
        }

        private void Initialize()
        {
            if (_isInitialized)
                return;

            Canvas.ForceUpdateCanvases();
            _shownPosition = _rectTransform.anchoredPosition;
            _rectTransform.anchoredPosition = _shownPosition + Vector2.up * hiddenOffsetY;
            _isInitialized = true;
        }

        private void OnDestroy()
        {
            _showTween?.Kill();
        }

        private void OnValidate()
        {
            duration = Mathf.Max(0f, duration);
            hiddenOffsetY = Mathf.Max(0f, hiddenOffsetY);
        }
    }
}
