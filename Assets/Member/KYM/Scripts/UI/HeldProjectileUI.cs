using Member.KYM.Scripts.CombatSystems.Projectiles;
using KimLIb.EventSystem;
using Member.KYM.Scripts.CoreSystems.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Member.KYM.Scripts.UI
{
    public class HeldProjectileUI : MonoBehaviour
    {
        [Header("플레이어 UI 채널")]
        [SerializeField] private EventChannelSO uiChannel;

        [Header("표시할 UI")]
        [SerializeField] private CanvasGroup displayGroup;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;

        private void OnEnable()
        {
            Show(null);
            if (uiChannel == null) return;
            uiChannel.AddListener<PlayerUIStateEvent>(HandleState);
            uiChannel.RaiseEvent(new PlayerUIStateRequest());
        }

        private void OnDisable()
        {
            if (uiChannel != null)
                uiChannel.RemoveListener<PlayerUIStateEvent>(HandleState);

            Show(null);
        }

        private void HandleState(PlayerUIStateEvent evt) => Show(evt.HeldProjectile);

        // 잡기 로직을 통하지 않고 데이터만 전달해도 표시할 수 있다.
        public void Show(ProjectileDataSO data)
        {
            if (displayGroup != null)
            {
                displayGroup.alpha = data != null ? 1f : 0f;
                displayGroup.interactable = false;
                displayGroup.blocksRaycasts = false;
            }

            if (iconImage != null)
            {
                iconImage.sprite = data != null ? data.Icon : null;
                iconImage.enabled = data != null && data.Icon != null;
            }

            if (nameText != null)
                nameText.text = data != null ? data.Name : string.Empty;
        }
    }
}
