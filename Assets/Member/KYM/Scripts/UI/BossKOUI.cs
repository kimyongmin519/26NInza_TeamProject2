using DG.Tweening;
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
        [SerializeField, Min(0f)] private float backgroundFadeDuration = 0.2f;
        [SerializeField, Min(0f)] private float revealDuration = 0.65f;
        [SerializeField] private Vector2 shakeStrength = new(18f, 8f);
        [SerializeField, Min(1)] private int shakeVibrato = 18;

        private HealthModule _health;
        private Sequence _sequence;
        private Vector2 _imageStartPosition;
        private bool _isShown;

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

        [ContextMenu("KO 연출 미리보기")]
        public void Show()
        {
            if (_isShown || overlayRoot == null || darkBackground == null || koImage == null)
                return;

            _isShown = true;
            _sequence?.Kill();
            overlayRoot.SetActive(true);

            Color backgroundColor = darkBackground.color;
            backgroundColor.a = 0f;
            darkBackground.color = backgroundColor;

            koImage.type = Image.Type.Filled;
            koImage.fillMethod = Image.FillMethod.Horizontal;
            koImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            koImage.fillAmount = 0f;
            koImage.rectTransform.anchoredPosition = _imageStartPosition;
            koImage.enabled = koImage.sprite != null;

            _sequence = DOTween.Sequence().SetUpdate(true);
            _sequence.Append(darkBackground.DOFade(backgroundAlpha, backgroundFadeDuration));
            _sequence.Append(koImage.DOFillAmount(1f, revealDuration).SetEase(Ease.OutCubic));
            _sequence.Join(koImage.rectTransform.DOShakeAnchorPos(
                revealDuration, shakeStrength, shakeVibrato, 90f, false, true));
            _sequence.OnComplete(() => koImage.rectTransform.anchoredPosition = _imageStartPosition);
        }
    }
}
