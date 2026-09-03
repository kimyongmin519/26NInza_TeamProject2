using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Member.ODK.Scripts.UI
{
    public interface IHealthModuleExtra
    {
        void Setting(HealthModule module);
    }

    public class HealthBarUI : MonoBehaviour, IHealthModuleExtra
    {
        [Header("References")]
        [SerializeField] private RectTransform barObject;
        [SerializeField] private Image barHighLight;
        [SerializeField] private Image barFill;

        [Header("Fill")]
        [SerializeField] private float fillDuration = 0.2f;
        [SerializeField] private float highlightDelay = 0.5f;
        [SerializeField] private float highlightDuration = 0.25f;

        private Sequence currentFillSequence;
        private Sequence currentShakeSequence;

        private HealthModule healthModule;

        private Vector2 barOriginPosition;
        private Quaternion barOriginRotation;

        private void Awake()
        {
            barOriginPosition = barObject.anchoredPosition;
            barOriginRotation = barObject.localRotation;
        }

        public void Setting(HealthModule module)
        {
            if (healthModule != null)
            {
                healthModule.OnHealthChanged -= ChangeHealthBar;
            }

            healthModule = module;

            if (healthModule == null)
                return;

            healthModule.OnHealthChanged += ChangeHealthBar;

            float percent = Mathf.Clamp01(
                healthModule.CurrentHealth / healthModule.MaxHealth
            );

            barFill.fillAmount = percent;
            barHighLight.fillAmount = percent;
        }

        private void ChangeHealthBar(float current, float previous)
        {
            if (healthModule == null)
                return;

            float maxHealth = healthModule.MaxHealth;

            if (maxHealth <= 0f)
                return;

            float percent = Mathf.Clamp01(current / maxHealth);

            float damage = Mathf.Max(0f, previous - current);
            float damageRatio = damage / maxHealth;

            PlayFillAnimation(percent);

            if (damage > 0f)
            {
                PlayShake(damageRatio);
            }
        }

        private void PlayFillAnimation(float target)
        {
            currentFillSequence?.Kill();

            currentFillSequence = DOTween.Sequence();

            currentFillSequence
                .Append(
                    barFill
                        .DOFillAmount(target, fillDuration)
                        .SetEase(Ease.OutCubic)
                )
                .AppendInterval(highlightDelay)
                .Append(
                    barHighLight
                        .DOFillAmount(target, highlightDuration)
                        .SetEase(Ease.OutQuad)
                );
        }

        private void PlayShake(float damageRatio)
        {
            currentShakeSequence?.Kill();

            ResetBarTransform();

            // 데미지가 커질수록 조금 강해지지만 전체적으로 매우 약한 진동
            float ratio = Mathf.Clamp01(damageRatio * 2f);

            float positionStrength = Mathf.Lerp(0.5f, 3f, ratio);
            float rotationStrength = Mathf.Lerp(0.15f, 0.75f, ratio);

            currentShakeSequence = DOTween.Sequence();

            currentShakeSequence
                .Append(
                    barObject
                        .DOShakeAnchorPos(
                            0.12f,
                            positionStrength,
                            8,
                            60f,
                            false,
                            true,
                            ShakeRandomnessMode.Full
                        )
                        .SetEase(Ease.OutQuad)
                )
                .Join(
                    barObject
                        .DOShakeRotation(
                            0.12f,
                            new Vector3(0f, 0f, rotationStrength),
                            8,
                            60f,
                            false,
                            ShakeRandomnessMode.Full
                        )
                        .SetEase(Ease.OutQuad)
                )
                .AppendCallback(ResetBarTransform)
                .OnKill(ResetBarTransform);
        }

        private void ResetBarTransform()
        {
            if (barObject == null)
                return;

            barObject.anchoredPosition = barOriginPosition;
            barObject.localRotation = barOriginRotation;
        }

        private void OnDestroy()
        {
            if (healthModule != null)
            {
                healthModule.OnHealthChanged -= ChangeHealthBar;
            }

            currentFillSequence?.Kill();
            currentShakeSequence?.Kill();
        }
    }
}
