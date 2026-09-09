using TMPro;
using UnityEngine;

namespace Member.KYM.Scripts.CoreSystems.InteractSystems
{
    public class InteractionPromptView : MonoBehaviour
    {
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private TMP_Text actionLabel;
        [SerializeField] private RoundedOutlineProgressGraphic progressGraphic;
        [SerializeField] private float height = 64f;
        [SerializeField] private float textPadding = 18f;
        [SerializeField] private float maximumWidth = 360f;
        [SerializeField] private float collapseDuration = 0.12f;

        private float _expandedWidth;
        private float _currentWidth;
        private float _targetWidth;
        private float _startWidth;
        private float _animationTime;
        private bool _hasText;
        private bool _holding;
        private bool _isAnimating;

        private void Awake()
        {
            _currentWidth = panelRect != null
                ? panelRect.rect.width
                : height;
            _targetWidth = _currentWidth;
            SetProgress(0f);
        }

        private void Update()
        {
            if (panelRect == null || !_isAnimating)
                return;

            if (collapseDuration <= 0f)
            {
                _currentWidth = _targetWidth;
                _isAnimating = false;
                ApplyWidth();
                return;
            }

            _animationTime += Time.unscaledDeltaTime;
            float time = Mathf.Clamp01(_animationTime / collapseDuration);
            float easedTime = 1f - Mathf.Pow(1f - time, 3f);
            _currentWidth = Mathf.Lerp(_startWidth, _targetWidth, easedTime);

            if (time >= 1f)
            {
                _currentWidth = _targetWidth;
                _isAnimating = false;
            }

            ApplyWidth();
        }

        public void SetInteractionText(string text)
        {
            if (actionLabel == null)
                return;

            string value = text?.Trim() ?? string.Empty;
            _hasText = value.Length > 0;
            actionLabel.text = value;
            actionLabel.gameObject.SetActive(_hasText);

            float preferredWidth = _hasText
                ? actionLabel.preferredWidth + textPadding * 2f
                : 0f;

            _expandedWidth = Mathf.Clamp(
                height + preferredWidth,
                height,
                maximumWidth
            );

            _targetWidth = _holding ? height : _expandedWidth;
            _currentWidth = _targetWidth;
            _isAnimating = false;
            ApplyWidth();
        }

        public void SetVisible(bool visible)
        {
            if (promptRoot != null)
                promptRoot.SetActive(visible);
        }

        public void SetHolding(bool holding)
        {
            _holding = holding;

            if (actionLabel != null)
                actionLabel.gameObject.SetActive(!holding && _hasText);

            StartWidthAnimation(
                holding || !_hasText
                    ? height
                    : _expandedWidth
            );

            if (progressGraphic != null)
                progressGraphic.enabled = holding;
        }

        public void SetProgress(float progress)
        {
            if (progressGraphic != null)
                progressGraphic.Progress = Mathf.Clamp01(progress);
        }

        private void ApplyWidth()
        {
            if (panelRect == null || canvasRect == null)
                return;

            Vector2 size = new(_currentWidth, height);
            panelRect.sizeDelta = size;
            canvasRect.sizeDelta = size;

            if (actionLabel != null)
            {
                float range = Mathf.Max(1f, _expandedWidth - height);
                float alpha = Mathf.Clamp01((_currentWidth - height) / range);
                Color color = actionLabel.color;
                color.a = alpha;
                actionLabel.color = color;
            }
        }

        private void StartWidthAnimation(float width)
        {
            _startWidth = _currentWidth;
            _targetWidth = width;
            _animationTime = 0f;
            _isAnimating = !Mathf.Approximately(_startWidth, _targetWidth);

            if (!_isAnimating)
            {
                _currentWidth = _targetWidth;
                ApplyWidth();
            }
        }
    }
}
