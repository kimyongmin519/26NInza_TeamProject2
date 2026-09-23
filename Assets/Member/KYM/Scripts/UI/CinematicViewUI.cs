using DG.Tweening;
using UnityEngine;

namespace Member.KYM.Scripts.UI
{
    public class CinematicViewUI : MonoBehaviour
    {
        [Header("시네마틱 바")]
        [SerializeField] private RectTransform topBar;
        [SerializeField] private RectTransform bottomBar;
        [SerializeField, Min(0f)] private float barHeight = 120f;
        [SerializeField] private bool startVisible;

        [Header("등장 연출")]
        [SerializeField, Min(0f)] private float showDuration = 0.35f;
        [SerializeField] private Ease showEase = Ease.OutCubic;

        [Header("퇴장 연출")]
        [SerializeField, Min(0f)] private float hideDuration = 0.3f;
        [SerializeField] private Ease hideEase = Ease.InCubic;

        public bool IsVisible => _isVisible;

        private Sequence _sequence;
        private bool _isVisible;
        private bool _isInitialized;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            if (_isInitialized)
                ApplyStateImmediately(_isVisible);
        }

        private void OnDisable()
        {
            KillSequence();
        }

        private void OnDestroy()
        {
            KillSequence();
        }

        [ContextMenu("시네마틱 바 보이기")]
        public void Show()
        {
            Animate(true, showDuration, showEase);
        }

        [ContextMenu("시네마틱 바 숨기기")]
        public void Hide()
        {
            Animate(false, hideDuration, hideEase);
        }

        public void ShowImmediately()
        {
            SetVisibleImmediately(true);
        }

        public void HideImmediately()
        {
            SetVisibleImmediately(false);
        }

        public void SetVisible(bool visible)
        {
            if (visible)
                Show();
            else
                Hide();
        }

        private void Initialize()
        {
            if (topBar == null || bottomBar == null)
            {
                Debug.LogWarning("시네마틱 바 RectTransform이 할당되지 않았습니다.", this);
                return;
            }

            SetBarHeight(topBar);
            SetBarHeight(bottomBar);

            _isVisible = startVisible;
            _isInitialized = true;
            ApplyStateImmediately(_isVisible);
        }

        private void Animate(bool visible, float duration, Ease ease)
        {
            if (!_isInitialized)
            {
                Initialize();
                if (!_isInitialized)
                    return;
            }

            if (_isVisible == visible && _sequence?.IsActive() != true)
                return;

            _isVisible = visible;
            KillSequence();

            if (duration <= 0f)
            {
                ApplyStateImmediately(visible);
                return;
            }

            float topPositionY = visible ? 0f : barHeight;
            float bottomPositionY = visible ? 0f : -barHeight;

            _sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            _sequence.Join(
                topBar.DOAnchorPosY(topPositionY, duration).SetEase(ease));
            _sequence.Join(
                bottomBar.DOAnchorPosY(bottomPositionY, duration).SetEase(ease));
            _sequence.OnComplete(() => _sequence = null);
        }

        private void SetVisibleImmediately(bool visible)
        {
            if (!_isInitialized)
            {
                Initialize();
                if (!_isInitialized)
                    return;
            }

            _isVisible = visible;
            KillSequence();
            ApplyStateImmediately(visible);
        }

        private void ApplyStateImmediately(bool visible)
        {
            SetAnchoredPositionY(topBar, visible ? 0f : barHeight);
            SetAnchoredPositionY(bottomBar, visible ? 0f : -barHeight);
        }

        private void SetBarHeight(RectTransform bar)
        {
            bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, barHeight);
        }

        private static void SetAnchoredPositionY(RectTransform target, float y)
        {
            Vector2 position = target.anchoredPosition;
            position.y = y;
            target.anchoredPosition = position;
        }

        private void KillSequence()
        {
            _sequence?.Kill();
            _sequence = null;
        }

        private void OnValidate()
        {
            barHeight = Mathf.Max(0f, barHeight);
            showDuration = Mathf.Max(0f, showDuration);
            hideDuration = Mathf.Max(0f, hideDuration);
        }
    }
}
