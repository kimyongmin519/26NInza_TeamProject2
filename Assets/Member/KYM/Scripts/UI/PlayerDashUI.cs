using Member.KYM.Scripts.Players.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace Member.KYM.Scripts.UI
{
    public class PlayerDashUI : MonoBehaviour
    {
        [Header("대시 쿨타임 표시")]
        [SerializeField] private PlayerDashSkill dashSkill;
        [SerializeField] private Image cooldownFillImage;

        private void Awake()
        {
            if (cooldownFillImage != null)
            {
                cooldownFillImage.type = Image.Type.Filled;
                cooldownFillImage.fillMethod = Image.FillMethod.Radial360;
            }
        }

        private void Update()
        {
            if (cooldownFillImage == null)
                return;

            cooldownFillImage.fillAmount = dashSkill != null
                ? dashSkill.NormalizedRecharge
                : 0f;
        }

        public void Bind(PlayerDashSkill playerDashSkill)
        {
            dashSkill = playerDashSkill;
            UpdateFillAmountImmediately();
        }

        private void UpdateFillAmountImmediately()
        {
            if (cooldownFillImage == null)
                return;

            cooldownFillImage.fillAmount = dashSkill != null
                ? dashSkill.NormalizedRecharge
                : 0f;
        }
    }
}
