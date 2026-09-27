using DG.Tweening;
using Member.KYM.Scripts.CoreSystems.Managers;
using Member.KYM.Scripts.Enemies.Boss;
using Member.ODK.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace Member.KYM.Scripts.UI
{
    public sealed class BossKOUI : MonoBehaviour
    {
        [Header("대상")]
        [SerializeField] private BTAgentBoss boss;

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
            Bind(boss);
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.OnDeath -= Show;

            _sequence?.Kill();
            RestoreTime();
        }

        public void Bind(BTAgentBoss target)
        {
            if (_health != null)
                _health.OnDeath -= Show;

            boss = target;
            _health = boss != null ? boss.HealthModule : null;

            if (_health == null)
            {
                Debug.LogWarning("KO UI에 보스의 HealthModule이 연결되지 않았습니다.", this);
                return;
            }

            _health.OnDeath += Show;
            if (_health.IsDead)
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
                overlayRoot.SetActive(false);
            });
        }

        private void OnDisable()
        {
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
    }
}
