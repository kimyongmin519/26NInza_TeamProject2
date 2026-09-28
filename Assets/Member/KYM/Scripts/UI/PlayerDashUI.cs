using KimLIb.EventSystem;
using Member.KYM.Scripts.CoreSystems.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Member.KYM.Scripts.UI
{
    public class PlayerDashUI : MonoBehaviour
    {
        [Header("대시 쿨타임 표시")]
        [SerializeField] private EventChannelSO uiChannel;
        [SerializeField] private Image cooldownFillImage;

        private void Awake()
        {
            if (cooldownFillImage != null)
            {
                cooldownFillImage.type = Image.Type.Filled;
                cooldownFillImage.fillMethod = Image.FillMethod.Radial360;
            }
        }

        private void OnEnable()
        {
            if (cooldownFillImage != null) cooldownFillImage.fillAmount = 0f;
            if (uiChannel == null) return;
            uiChannel.AddListener<PlayerUIStateEvent>(HandleState);
            uiChannel.RaiseEvent(new PlayerUIStateRequest());
        }

        private void OnDisable()
        {
            if (uiChannel != null)
                uiChannel.RemoveListener<PlayerUIStateEvent>(HandleState);
        }

        private void HandleState(PlayerUIStateEvent evt)
        {
            if (cooldownFillImage == null)
                return;

            cooldownFillImage.fillAmount = evt.DashCharge;
        }
    }
}
