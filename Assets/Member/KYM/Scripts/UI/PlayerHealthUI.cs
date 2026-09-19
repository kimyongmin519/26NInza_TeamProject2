using System;
using KimLIb.EventSystem;
using Member.KYM.Scripts.CoreSystems.Events;
using Member.ODK.Scripts;
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

        [SerializeField] private EventChannelSO uiChannel;
        private HealthModule _playerHealthModule;

        private void Awake()
        {
            uiChannel.AddListener<PlayerHealthSubEvent>(HandleBindPlayerHealth);
        }

        private void OnDestroy()
        {
            uiChannel.RemoveListener<PlayerHealthSubEvent>(HandleBindPlayerHealth);
            if (_playerHealthModule != null)
                _playerHealthModule.OnHealthChanged -= HandleHealthChange;
        }

        private void HandleBindPlayerHealth(PlayerHealthSubEvent evt)
        {
            if (_playerHealthModule != null)
            {
                _playerHealthModule.OnHealthChanged -= HandleHealthChange;
            }
            
            _playerHealthModule = evt.PlayerHealthModule;
            _playerHealthModule.OnHealthChanged += HandleHealthChange;
        }

        private void HandleHealthChange(float current, float max)
        {
            healthText.SetText(current.ToString());
        }
    }
}