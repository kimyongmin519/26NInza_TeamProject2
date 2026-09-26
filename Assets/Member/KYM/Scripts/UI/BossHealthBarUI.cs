using DG.Tweening;
using Member.KYM.Scripts.Enemies.Boss;
using Member.ODK.Scripts;
using UnityEngine;

namespace Member.KYM.Scripts.UI
{
    public class BossHealthBarUI : MonoBehaviour
    {
        [Header("보스")]
        [SerializeField] private BTAgentBoss boss;
        [Tooltip("Optional health source for bosses that do not use BTAgentBoss.")]
        [SerializeField] private HealthModule healthSource;

        [Header("막대 마스크")]
        [SerializeField] private RectTransform fillMask;
        [SerializeField] private RectTransform lagMask;

        [Header("피해 잔상")]
        [SerializeField, Min(0f)] private float lagDelay = 0.2f;
        [SerializeField, Min(0f)] private float lagDuration = 0.45f;

        private HealthModule _health;
        private Tween _lagTween;
        private float _fullWidth;
        private float _lagFraction = 1f;

        private void Awake()
        {
            if (fillMask == null || lagMask == null)
            {
                Debug.LogError("보스 체력바의 Fill/Lag 마스크를 연결해야 합니다.", this);
                enabled = false;
                return;
            }

            _fullWidth = fillMask.rect.width;
        }

        private void Start()
        {
            if (healthSource != null) BindHealth(healthSource);
            else Bind(boss);
        }

        private void OnEnable()
        {
            if (_health == null)
                return;

            _health.OnHealthChanged += HandleHealthChanged;
            RefreshImmediately();
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.OnHealthChanged -= HandleHealthChanged;

            StopLagTween();
        }

        public void Bind(BTAgentBoss target)
        {
            boss = target;
            BindHealth(target != null ? target.HealthModule : null);
        }

        public void BindHealth(HealthModule target)
        {
            if (_health != null)
                _health.OnHealthChanged -= HandleHealthChanged;

            StopLagTween();
            healthSource = target;
            _health = target;

            if (_health == null)
            {
                Debug.LogWarning("보스의 HealthModule을 찾지 못했습니다.", this);
                return;
            }

            if (isActiveAndEnabled)
                _health.OnHealthChanged += HandleHealthChanged;

            RefreshImmediately();
        }

        private void RefreshImmediately()
        {
            if (_health == null)
                return;

            float fraction = GetHealthFraction(_health.CurrentHealth, _health.MaxHealth);
            SetFillFraction(fraction);
            SetLagFraction(fraction);
        }

        private void HandleHealthChanged(float current, float max)
        {
            float fraction = GetHealthFraction(current, max);
            SetFillFraction(fraction);

            StopLagTween();
            if (fraction >= _lagFraction || lagDuration <= 0f)
            {
                SetLagFraction(fraction);
                return;
            }

            _lagTween = DOTween.To(
                    () => _lagFraction,
                    SetLagFraction,
                    fraction,
                    lagDuration)
                .SetDelay(lagDelay)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .OnComplete(() => _lagTween = null);
        }

        private void SetFillFraction(float fraction)
        {
            fillMask.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                _fullWidth * Mathf.Clamp01(fraction));
        }

        private void SetLagFraction(float fraction)
        {
            _lagFraction = Mathf.Clamp01(fraction);
            lagMask.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                _fullWidth * _lagFraction);
        }

        private void StopLagTween()
        {
            _lagTween?.Kill();
            _lagTween = null;
        }

        private static float GetHealthFraction(float current, float max)
        {
            return max > 0f ? Mathf.Clamp01(current / max) : 0f;
        }
    }
}
