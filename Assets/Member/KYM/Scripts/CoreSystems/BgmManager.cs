using System.Collections;
using DG.Tweening;
using KimLIb.SoundSystem;
using UnityEngine;
using UnityEngine.Audio;

namespace Member.KYM.Scripts.CoreSystems
{
    [DisallowMultipleComponent]
    public sealed class BgmManager : MonoBehaviour
    {
        [Header("출력")]
        [SerializeField] private AudioMixerGroup bgmGroup;
        [SerializeField, Min(0f)] private float defaultFadeDuration = 0.7f;

        [Header("시작 음악")]
        [SerializeField] private SoundClipSO startingBgm;

        public static BgmManager Instance { get; private set; }
        public SoundClipSO CurrentBgm { get; private set; }

        private AudioSource _activeSource;
        private AudioSource _standbySource;
        private Tween _transition;
        private Coroutine _loadRoutine;
        private SoundClipSO _pendingBgm;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _activeSource = CreateSource("BGM A");
            _standbySource = CreateSource("BGM B");

            if (bgmGroup == null)
                Debug.LogWarning("BgmManager에 AudioMixer의 Bgm 그룹을 연결해야 BGM 볼륨 설정이 적용됩니다.", this);
        }

        private void Start()
        {
            if (startingBgm != null)
                PlayBgm(startingBgm);
        }

        public void PlayBgm(SoundClipSO bgm)
        {
            PlayBgm(bgm, defaultFadeDuration);
        }

        public void PlayBgm(SoundClipSO bgm, float fadeDuration)
        {
            if (bgm == null || bgm.audioClip == null)
                return;

            if (bgm.audioTypes != AudioTypes.Music)
            {
                Debug.LogWarning($"{bgm.name}은 Music 타입이 아니어서 BGM으로 재생하지 않습니다.", this);
                return;
            }

            if ((CurrentBgm == bgm && _activeSource.isPlaying) || _pendingBgm == bgm)
                return;

            CancelPendingLoad();
            AudioClip clip = bgm.audioClip;
            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                if (clip.loadState == AudioDataLoadState.Unloaded)
                    clip.LoadAudioData();

                _pendingBgm = bgm;
                _loadRoutine = StartCoroutine(PlayWhenLoaded(bgm, fadeDuration));
                return;
            }

            BeginTransition(bgm, fadeDuration);
        }

        private IEnumerator PlayWhenLoaded(SoundClipSO bgm, float fadeDuration)
        {
            AudioClip clip = bgm.audioClip;
            while (clip != null && clip.loadState == AudioDataLoadState.Loading)
                yield return null;

            _loadRoutine = null;
            _pendingBgm = null;
            if (clip == null || clip.loadState != AudioDataLoadState.Loaded)
            {
                Debug.LogWarning($"BGM '{bgm.name}'의 오디오 데이터를 불러오지 못했습니다.", this);
                yield break;
            }

            BeginTransition(bgm, fadeDuration);
        }

        private void BeginTransition(SoundClipSO bgm, float fadeDuration)
        {
            KillTransition();

            AudioSource outgoing = _activeSource;
            AudioSource incoming = _standbySource;
            incoming.Stop();
            incoming.clip = bgm.audioClip;
            incoming.loop = bgm.loop;
            incoming.pitch = bgm.pitch;
            incoming.outputAudioMixerGroup = bgmGroup;

            float targetVolume = Mathf.Clamp01(bgm.volume);
            float duration = Mathf.Max(0f, fadeDuration);
            incoming.volume = duration > 0f ? 0f : targetVolume;
            incoming.Play();

            _activeSource = incoming;
            _standbySource = outgoing;
            CurrentBgm = bgm;

            if (duration <= 0f)
            {
                StopSource(outgoing);
                return;
            }

            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Join(FadeVolume(incoming, targetVolume, duration));
            if (outgoing.isPlaying)
                sequence.Join(FadeVolume(outgoing, 0f, duration));
            sequence.OnComplete(() =>
            {
                StopSource(outgoing);
                _transition = null;
            });
            _transition = sequence;
        }

        public void StopBgm()
        {
            StopBgm(defaultFadeDuration);
        }

        public void StopBgm(float fadeDuration)
        {
            CancelPendingLoad();
            KillTransition();
            CurrentBgm = null;

            float duration = Mathf.Max(0f, fadeDuration);
            if (duration <= 0f || !_activeSource.isPlaying && !_standbySource.isPlaying)
            {
                StopSource(_activeSource);
                StopSource(_standbySource);
                return;
            }

            AudioSource active = _activeSource;
            AudioSource standby = _standbySource;
            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            if (active.isPlaying)
                sequence.Join(FadeVolume(active, 0f, duration));
            if (standby.isPlaying)
                sequence.Join(FadeVolume(standby, 0f, duration));
            sequence.OnComplete(() =>
            {
                StopSource(active);
                StopSource(standby);
                _transition = null;
            });
            _transition = sequence;
        }

        private AudioSource CreateSource(string sourceName)
        {
            GameObject sourceObject = new(sourceName);
            sourceObject.transform.SetParent(transform, false);

            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = bgmGroup;
            source.volume = 0f;
            return source;
        }

        private static void StopSource(AudioSource source)
        {
            source.Stop();
            source.clip = null;
            source.volume = 0f;
        }

        private static Tween FadeVolume(AudioSource source, float targetVolume, float duration)
        {
            return DOTween.To(() => source.volume, volume => source.volume = volume,
                targetVolume, duration);
        }

        private void KillTransition()
        {
            _transition?.Kill();
            _transition = null;
        }

        private void CancelPendingLoad()
        {
            if (_loadRoutine != null)
                StopCoroutine(_loadRoutine);

            _loadRoutine = null;
            _pendingBgm = null;
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            CancelPendingLoad();
            KillTransition();
            Instance = null;
        }

        private void OnValidate()
        {
            defaultFadeDuration = Mathf.Max(0f, defaultFadeDuration);
        }
    }
}
