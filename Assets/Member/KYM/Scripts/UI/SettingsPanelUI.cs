using System.Collections.Generic;
using Member.KYM.Scripts.CoreSystems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Member.KYM.Scripts.UI
{
    public sealed class SettingsPanelUI : MonoBehaviour
    {
        [Header("설정 서비스")]
        [SerializeField] private GameSettingsService settings;

        [Header("사운드 (슬라이더 범위 0~1)")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider bgmSlider;

        [Header("화면")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private GameObject displayConfirmPanel;

        private void OnEnable()
        {
            if (settings == null)
                settings = GameSettingsService.Instance;

            if (settings == null || masterSlider == null || sfxSlider == null ||
                bgmSlider == null || resolutionDropdown == null || fullscreenToggle == null)
            {
                Debug.LogError("설정창의 서비스와 UI 참조를 연결해야 합니다.", this);
                enabled = false;
                return;
            }

            PopulateResolutions();
            RefreshAudio();
            RefreshDisplay();

            masterSlider.onValueChanged.AddListener(settings.SetMasterVolume);
            sfxSlider.onValueChanged.AddListener(settings.SetSfxVolume);
            bgmSlider.onValueChanged.AddListener(settings.SetBgmVolume);
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            settings.DisplaySettingsChanged += RefreshDisplay;
        }

        private void OnDisable()
        {
            if (settings == null)
                return;

            if (masterSlider != null)
                masterSlider.onValueChanged.RemoveListener(settings.SetMasterVolume);
            if (sfxSlider != null)
                sfxSlider.onValueChanged.RemoveListener(settings.SetSfxVolume);
            if (bgmSlider != null)
                bgmSlider.onValueChanged.RemoveListener(settings.SetBgmVolume);
            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);

            settings.DisplaySettingsChanged -= RefreshDisplay;
            settings.SaveAudio();
        }

        private void OnResolutionChanged(int _) => ApplyDisplay();
        private void OnFullscreenChanged(bool _) => ApplyDisplay();

        // 해상도 또는 전체화면 값을 바꾸면 즉시 적용한다.
        public void ApplyDisplay()
        {
            IReadOnlyList<Vector2Int> resolutions = settings.SupportedResolutions;
            int selectedIndex = resolutionDropdown.value;
            if (selectedIndex < 0 || selectedIndex >= resolutions.Count)
                return;

            Vector2Int selected = resolutions[selectedIndex];
            settings.TryApplyDisplay(selected.x, selected.y, fullscreenToggle.isOn);
        }

        // 변경 확인 버튼에 연결한다.
        public void ConfirmDisplay()
        {
            settings.ConfirmDisplay();
        }

        // 취소 버튼에 연결한다. 확인하지 않아도 제한 시간이 지나면 자동으로 되돌아간다.
        public void RevertDisplay()
        {
            settings.RevertDisplay();
        }

        private void PopulateResolutions()
        {
            resolutionDropdown.ClearOptions();

            List<TMP_Dropdown.OptionData> options = new();
            foreach (Vector2Int size in settings.SupportedResolutions)
                options.Add(new TMP_Dropdown.OptionData($"{size.x} × {size.y}"));

            resolutionDropdown.AddOptions(options);
        }

        private void RefreshAudio()
        {
            masterSlider.SetValueWithoutNotify(settings.MasterVolume);
            sfxSlider.SetValueWithoutNotify(settings.SfxVolume);
            bgmSlider.SetValueWithoutNotify(settings.BgmVolume);
        }

        private void RefreshDisplay()
        {
            Vector2Int currentSize = new(settings.ScreenWidth, settings.ScreenHeight);
            int index = 0;
            for (int i = 0; i < settings.SupportedResolutions.Count; i++)
            {
                if (settings.SupportedResolutions[i] != currentSize)
                    continue;

                index = i;
                break;
            }

            resolutionDropdown.SetValueWithoutNotify(index);
            resolutionDropdown.RefreshShownValue();
            fullscreenToggle.SetIsOnWithoutNotify(settings.IsFullscreen);

            if (displayConfirmPanel != null)
                displayConfirmPanel.SetActive(settings.HasPendingDisplayConfirmation);
        }
    }
}
