using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Member.KYM.Scripts.CoreSystems
{
    [DefaultExecutionOrder(-500)]
    public sealed class GameSettingsService : MonoBehaviour
    {
        private const string MasterKey = "KYM.Settings.MasterVolume";
        private const string SfxKey = "KYM.Settings.SfxVolume";
        private const string BgmKey = "KYM.Settings.BgmVolume";
        private const string WidthKey = "KYM.Settings.ScreenWidth";
        private const string HeightKey = "KYM.Settings.ScreenHeight";
        private const string FullscreenKey = "KYM.Settings.Fullscreen";

        [Header("오디오 믹서의 Exposed Parameters 이름")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string masterParameter = "MasterVolume";
        [SerializeField] private string sfxParameter = "SfxVolume";
        [SerializeField] private string bgmParameter = "BgmVolume";

        [Header("해상도 변경 확인")]
        [SerializeField, Min(1f)] private float displayConfirmSeconds = 12f;

        public static GameSettingsService Instance { get; private set; }

        public float MasterVolume { get; private set; } = 1f;
        public float SfxVolume { get; private set; } = 1f;
        public float BgmVolume { get; private set; } = 1f;
        public int ScreenWidth { get; private set; }
        public int ScreenHeight { get; private set; }
        public bool IsFullscreen { get; private set; }
        public bool HasPendingDisplayConfirmation => _confirmRoutine != null;
        public IReadOnlyList<Vector2Int> SupportedResolutions => _supportedResolutions;

        public event Action DisplaySettingsChanged;

        private readonly List<Vector2Int> _supportedResolutions = new();
        private readonly HashSet<string> _missingMixerParameters = new();
        private Coroutine _confirmRoutine;
        private Vector2Int _previousResolution;
        private bool _previousFullscreen;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            // 설정 UI 프리팹의 자식으로 배치해도 서비스만 씬 사이에 유지한다.
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            BuildResolutionList();
            LoadSettings();
        }

        private void Start()
        {
            ApplyAllAudio();
            ApplyDisplay(ScreenWidth, ScreenHeight, IsFullscreen);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnApplicationQuit()
        {
            SaveAudio();
        }

        public void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            ApplyVolume(masterParameter, MasterVolume);
        }

        public void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            ApplyVolume(sfxParameter, SfxVolume);
        }

        public void SetBgmVolume(float value)
        {
            BgmVolume = Mathf.Clamp01(value);
            ApplyVolume(bgmParameter, BgmVolume);
        }

        public void SaveAudio()
        {
            PlayerPrefs.SetFloat(MasterKey, MasterVolume);
            PlayerPrefs.SetFloat(SfxKey, SfxVolume);
            PlayerPrefs.SetFloat(BgmKey, BgmVolume);
            PlayerPrefs.Save();
        }

        public bool TryApplyDisplay(int width, int height, bool fullscreen)
        {
            Vector2Int resolution = new(width, height);
            if (!_supportedResolutions.Contains(resolution))
                return false;

            if (_confirmRoutine == null)
            {
                _previousResolution = new Vector2Int(ScreenWidth, ScreenHeight);
                _previousFullscreen = IsFullscreen;
            }
            else
            {
                StopCoroutine(_confirmRoutine);
                _confirmRoutine = null;
            }

            ApplyDisplay(width, height, fullscreen);
            _confirmRoutine = StartCoroutine(WaitForDisplayConfirmation());
            DisplaySettingsChanged?.Invoke();
            return true;
        }

        public void ConfirmDisplay()
        {
            if (_confirmRoutine == null)
                return;

            StopCoroutine(_confirmRoutine);
            _confirmRoutine = null;
            SaveDisplay();
            DisplaySettingsChanged?.Invoke();
        }

        public void RevertDisplay()
        {
            if (_confirmRoutine == null)
                return;

            StopCoroutine(_confirmRoutine);
            _confirmRoutine = null;
            ApplyDisplay(_previousResolution.x, _previousResolution.y, _previousFullscreen);
            DisplaySettingsChanged?.Invoke();
        }

        private IEnumerator WaitForDisplayConfirmation()
        {
            yield return new WaitForSecondsRealtime(displayConfirmSeconds);
            _confirmRoutine = null;
            ApplyDisplay(_previousResolution.x, _previousResolution.y, _previousFullscreen);
            DisplaySettingsChanged?.Invoke();
        }

        private void LoadSettings()
        {
            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterKey, 1f));
            SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, 1f));
            BgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmKey, 1f));

            Vector2Int savedResolution = new(
                PlayerPrefs.GetInt(WidthKey, Screen.width),
                PlayerPrefs.GetInt(HeightKey, Screen.height));
            if (!_supportedResolutions.Contains(savedResolution))
                savedResolution = new Vector2Int(Screen.width, Screen.height);

            ScreenWidth = savedResolution.x;
            ScreenHeight = savedResolution.y;
            IsFullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) != 0;
        }

        private void BuildResolutionList()
        {
            _supportedResolutions.Clear();
            foreach (Resolution resolution in Screen.resolutions)
            {
                Vector2Int size = new(resolution.width, resolution.height);
                if (!_supportedResolutions.Contains(size))
                    _supportedResolutions.Add(size);
            }

            Vector2Int currentSize = new(Screen.width, Screen.height);
            if (!_supportedResolutions.Contains(currentSize))
                _supportedResolutions.Add(currentSize);

            _supportedResolutions.Sort((a, b) =>
            {
                int widthOrder = a.x.CompareTo(b.x);
                return widthOrder != 0 ? widthOrder : a.y.CompareTo(b.y);
            });
        }

        private void ApplyAllAudio()
        {
            ApplyVolume(masterParameter, MasterVolume);
            ApplyVolume(sfxParameter, SfxVolume);
            ApplyVolume(bgmParameter, BgmVolume);
        }

        private void ApplyVolume(string parameterName, float volume)
        {
            if (audioMixer == null || string.IsNullOrEmpty(parameterName))
                return;

            float decibels = volume <= 0.001f
                ? -80f
                : Mathf.Log10(volume) * 20f;

            if (!audioMixer.SetFloat(parameterName, decibels) &&
                _missingMixerParameters.Add(parameterName))
                Debug.LogWarning($"AudioMixer에 노출된 '{parameterName}' 파라미터가 없습니다.", this);
        }

        private void ApplyDisplay(int width, int height, bool fullscreen)
        {
            ScreenWidth = width;
            ScreenHeight = height;
            IsFullscreen = fullscreen;

            FullScreenMode mode = fullscreen
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;
            Screen.SetResolution(width, height, mode);
        }

        private void SaveDisplay()
        {
            PlayerPrefs.SetInt(WidthKey, ScreenWidth);
            PlayerPrefs.SetInt(HeightKey, ScreenHeight);
            PlayerPrefs.SetInt(FullscreenKey, IsFullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
