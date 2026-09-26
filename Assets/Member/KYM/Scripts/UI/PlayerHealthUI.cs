using System;
using DG.Tweening;
using KimLIb.EventSystem;
using Member.KYM.Scripts.CoreSystems.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Member.KYM.Scripts.UI
{
    public class PlayerHealthUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Image heartImage;
        [SerializeField] private Sprite[] heartSprites;

        [Header("체력 1 흔들림 연출")]
        [SerializeField, Min(0f)] private float shakeDuration = 0.25f;
        [SerializeField, Min(0f)] private float shakeStrength = 6f;
        [SerializeField, Min(1)] private int shakeVibrato = 12;

        [SerializeField] private EventChannelSO uiChannel;
        private float _displayedHealth = float.NaN;
        private Tween _heartShakeTween;
        private Vector2 _heartStartPosition;

        private void Awake()
        {
            if (heartImage != null)
                _heartStartPosition = heartImage.rectTransform.anchoredPosition;
        }

        private void OnDisable()
        {
            StopHeartShake();
            if (uiChannel != null)
                uiChannel.RemoveListener<PlayerUIStateEvent>(HandleState);
        }

        private void OnEnable()
        {
            _displayedHealth = float.NaN;
            if (uiChannel == null) return;
            uiChannel.AddListener<PlayerUIStateEvent>(HandleState);
            uiChannel.RaiseEvent(new PlayerUIStateRequest());
        }

        private void HandleState(PlayerUIStateEvent evt)
        {
            if (_displayedHealth == evt.Health) return;
            _displayedHealth = evt.Health;
            HandleHealthChange(evt.Health, evt.MaxHealth);
        }

        private void HandleHealthChange(float current, float max)
        {
            if (healthText != null) healthText.SetText(current.ToString());
            UpdateHeartSprite(current);

            if (current > 0f && current <= 1f)
                StartHeartShake();
            else
                StopHeartShake();
        }

        private void UpdateHeartSprite(float currentHealth)
        {
            if (heartImage == null || heartSprites == null || heartSprites.Length < 3)
                return;

            int spriteIndex = currentHealth <= 0f
                ? 2
                : currentHealth <= 2f
                    ? 1
                    : 0;

            heartImage.sprite = heartSprites[spriteIndex];
        }

        private void StartHeartShake()
        {
            if (heartImage == null ||
                _heartShakeTween?.IsActive() == true ||
                shakeDuration <= 0f ||
                shakeStrength <= 0f)
            {
                return;
            }

            RectTransform heartTransform = heartImage.rectTransform;
            heartTransform.anchoredPosition = _heartStartPosition;
            _heartShakeTween = heartTransform
                .DOShakeAnchorPos(
                    shakeDuration,
                    Vector2.one * shakeStrength,
                    shakeVibrato,
                    180f,
                    false,
                    false)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true);
        }

        private void StopHeartShake()
        {
            _heartShakeTween?.Kill();
            _heartShakeTween = null;

            if (heartImage != null)
                heartImage.rectTransform.anchoredPosition = _heartStartPosition;
        }
    }
}
