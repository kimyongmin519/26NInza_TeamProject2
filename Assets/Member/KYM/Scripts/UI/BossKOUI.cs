using DG.Tweening;
using KimLIb.EventSystem;
using KimLIb.SoundSystem;
using Member.KYM.Scripts.CoreSystems;
using Member.KYM.Scripts.CoreSystems.Managers;
using Member.ODK.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace Member.KYM.Scripts.UI
{
    public sealed class BossKOUI : MonoBehaviour
    {
        [Header("처치 연출을 표시할 체력 모듈")]
        [SerializeField] private HealthModule healthModule;

        [Header("UI")]
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Image darkBackground;
        [SerializeField] private Image koImage;

        [Header("등장 연출")]
        [SerializeField, Range(0f, 1f)] private float backgroundAlpha = 0.7f;
        [SerializeField, Min(0f)] private float revealDuration = 0.65f;
        [SerializeField, Min(0f)] private float holdDuration = 0.6f;

        [Header("짧고 강한 KO 흔들림")]
        [SerializeField, Min(0f)] private float shakeDuration = 0.18f;
        [SerializeField] private Vector2 shakeStrength = new(30f, 30f);
        [SerializeField, Min(1)] private int shakeVibrato = 24;

        [Header("KO 사운드")]
        [SerializeField] private EventChannelSO soundChannel;
        [SerializeField] private SoundClipSO koSound;
        [SerializeField] private SoundClipSO additionalKoSound;

        [Header("연출 종료 후 이동할 씬 (비워두면 이동하지 않음)")]
        [SerializeField] private string destinationScene;

        private HealthModule _health;
        private Sequence _sequence;
        private Vector2 _imageStartPosition;
        private bool _isShown;
        private bool _ownsTimeStop;

        private void Awake()
        {
            if (overlayRoot == null || darkBackground == null || koImage == null)
            {
                Debug.LogError("BossKOUI의 오버레이, 배경, KO 이미지를 연결해야 합니다.", this);
                enabled = false;
                return;
            }

            _imageStartPosition = koImage.rectTransform.anchoredPosition;
            overlayRoot.SetActive(false);
        }

        private void Start()
        {
            Bind(healthModule);
        }

        private void OnEnable()
        {
            if (_health == null) return;
            _health.OnDeath += Show;
            if (_health.IsDead) Show();
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.OnDeath -= Show;

            _sequence?.Kill();
            RestoreTime();
        }

        public void Bind(HealthModule target)
        {
            if (_health != null)
                _health.OnDeath -= Show;

            healthModule = target;
            _health = target;

            if (_health == null)
            {
                Debug.LogWarning("KO UI에 HealthModule을 연결해주세요.", this);
                return;
            }

            if (isActiveAndEnabled)
                _health.OnDeath += Show;
            if (isActiveAndEnabled && _health.IsDead)
                Show();
        }
        
        public void Show()
        {
            if (_isShown || overlayRoot == null || darkBackground == null || koImage == null)
                return;

            _isShown = true;
            _sequence?.Kill();
            overlayRoot.SetActive(true);

            darkBackground.color = new Color(0f, 0f, 0f, backgroundAlpha);
            koImage.fillAmount = 0f;
            koImage.rectTransform.anchoredPosition = _imageStartPosition;

            PlaySound(koSound);
            PlaySound(additionalKoSound);

            TimeManager.Instance.StopTimer();
            _ownsTimeStop = true;
            _sequence = DOTween.Sequence().SetUpdate(true);
            _sequence.Append(koImage.DOFillAmount(1f, revealDuration).SetEase(Ease.OutCubic));
            _sequence.Join(koImage.rectTransform.DOShakeAnchorPos(
                shakeDuration, shakeStrength, shakeVibrato, 180f, false, true));
            _sequence.AppendInterval(holdDuration);
            _sequence.OnComplete(() =>
            {
                koImage.rectTransform.anchoredPosition = _imageStartPosition;
                RestoreTime();

                if (string.IsNullOrWhiteSpace(destinationScene) ||
                    !SceneLoadManager.TryLoadScene(destinationScene))
                    overlayRoot.SetActive(false);
            });
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.OnDeath -= Show;
            _sequence?.Kill();
            RestoreTime();
        }

        private void RestoreTime()
        {
            if (!_ownsTimeStop)
                return;

            _ownsTimeStop = false;
            if (TimeManager.Instance != null)
                TimeManager.Instance.StartTimer();
        }

        private void PlaySound(SoundClipSO clip)
        {
            if (soundChannel == null || clip == null || clip.audioClip == null)
                return;

            soundChannel.RaiseEvent(
                SoundEvent.PlaySoundEvent.InitData(Vector3.zero, clip));
        }
    }
}
