using DG.Tweening;
using Member.KYM.Scripts.CoreSystems;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;

namespace Member.KYM.Scripts.UI
{
    // UI 오브젝트는 모두 씬에 배치한다. 이 컴포넌트는 연결과 슬라이드 연출만 담당한다.
    [MovedFrom(true, "Member.KYM.Scripts.UI.MainTitle", null, "TitleSettingsDrawer")]
    public sealed class SettingsDrawer : MonoBehaviour
    {
        [Header("설정")]
        [SerializeField] private GameSettingsService settings;

        [Header("패널")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private GameObject backdrop;
        [FormerlySerializedAs("titleSettingButton")]
        [SerializeField] private Button openButton;
        [SerializeField] private Button backdropButton;
        [SerializeField] private Button closeButton;

        [Header("사운드")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider bgmSlider;

        [Header("화면")]
        [SerializeField] private TMP_Text resolutionText;
        [SerializeField] private Button previousResolutionButton;
        [SerializeField] private Button nextResolutionButton;
        [SerializeField] private Toggle fullscreenToggle;


        [Header("등장 연출")]
        [SerializeField, Min(0f)] private float slideDuration = 0.4f;
        [SerializeField] private Ease slideEase = Ease.OutCubic;

        private int _resolutionIndex;
        private bool _isOpen;

        private void Awake()
        {
            if (GameSettingsService.Instance != null)
                settings = GameSettingsService.Instance;

            if (settings == null || panel == null || backdrop == null ||
                backdropButton == null || closeButton == null || masterSlider == null ||
                sfxSlider == null || bgmSlider == null || resolutionText == null ||
                previousResolutionButton == null || nextResolutionButton == null ||
                fullscreenToggle == null)
            {
                Debug.LogError("설정 패널의 서비스 또는 UI 참조가 누락됐습니다.", this);
                enabled = false;
                return;
            }

            Vector2 position = panel.anchoredPosition;
            position.x = ClosedPositionX;
            panel.anchoredPosition = position;
            backdrop.SetActive(false);

            masterSlider.SetValueWithoutNotify(settings.MasterVolume);
            sfxSlider.SetValueWithoutNotify(settings.SfxVolume);
            bgmSlider.SetValueWithoutNotify(settings.BgmVolume);
            RefreshDisplay();

            if (openButton != null) openButton.onClick.AddListener(Open);
            backdropButton.onClick.AddListener(Close);
            closeButton.onClick.AddListener(Close);
            masterSlider.onValueChanged.AddListener(settings.SetMasterVolume);
            sfxSlider.onValueChanged.AddListener(settings.SetSfxVolume);
            bgmSlider.onValueChanged.AddListener(settings.SetBgmVolume);
            previousResolutionButton.onClick.AddListener(PreviousResolution);
            nextResolutionButton.onClick.AddListener(NextResolution);
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            settings.DisplaySettingsChanged += RefreshDisplay;
        }

        private void OnDestroy()
        {
            if (panel != null)
                panel.DOKill();

            if (openButton != null) openButton.onClick.RemoveListener(Open);
            if (backdropButton != null) backdropButton.onClick.RemoveListener(Close);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (previousResolutionButton != null) previousResolutionButton.onClick.RemoveListener(PreviousResolution);
            if (nextResolutionButton != null) nextResolutionButton.onClick.RemoveListener(NextResolution);
            if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);

            if (settings == null)
                return;

            if (masterSlider != null) masterSlider.onValueChanged.RemoveListener(settings.SetMasterVolume);
            if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(settings.SetSfxVolume);
            if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(settings.SetBgmVolume);
            settings.DisplaySettingsChanged -= RefreshDisplay;
            settings.SaveAudio();
        }

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
                return;

            if (_isOpen)
                Close();
            else
                Open();
        }

        private float ClosedPositionX => panel.rect.width + 20f;

        public void Open()
        {
            if (_isOpen)
                return;

            _isOpen = true;
            RefreshDisplay();
            backdrop.SetActive(true);
            panel.DOKill();
            panel.DOAnchorPosX(0f, slideDuration).SetEase(slideEase).SetUpdate(true);
        }

        public void Close()
        {
            if (!_isOpen)
                return;

            _isOpen = false;
            settings.SaveAudio();
            panel.DOKill();
            panel.DOAnchorPosX(ClosedPositionX, slideDuration)
                .SetEase(Ease.InCubic).SetUpdate(true)
                .OnComplete(() => backdrop.SetActive(false));
        }

        private void PreviousResolution() => ChangeResolution(-1);
        private void NextResolution() => ChangeResolution(1);

        private void ChangeResolution(int direction)
        {
            int count = settings.SupportedResolutions.Count;
            if (count == 0)
                return;

            _resolutionIndex = (_resolutionIndex + direction + count) % count;
            ApplyDisplay();
        }

        private void OnFullscreenChanged(bool _) => ApplyDisplay();

        private void ApplyDisplay()
        {
            if (_resolutionIndex < 0 || _resolutionIndex >= settings.SupportedResolutions.Count)
                return;

            Vector2Int size = settings.SupportedResolutions[_resolutionIndex];
            settings.TryApplyDisplay(size.x, size.y, fullscreenToggle.isOn);
        }

        private void RefreshDisplay()
        {
            if (settings == null || resolutionText == null)
                return;

            Vector2Int current = new(settings.ScreenWidth, settings.ScreenHeight);
            for (int i = 0; i < settings.SupportedResolutions.Count; i++)
            {
                if (settings.SupportedResolutions[i] != current)
                    continue;

                _resolutionIndex = i;
                break;
            }

            UpdateResolutionText();
            fullscreenToggle.SetIsOnWithoutNotify(settings.IsFullscreen);
        }

        private void UpdateResolutionText()
        {
            if (settings.SupportedResolutions.Count == 0)
            {
                resolutionText.text = "N/A";
                return;
            }

            Vector2Int size = settings.SupportedResolutions[_resolutionIndex];
            resolutionText.text = $"{size.x} × {size.y}";
        }
    }
}
