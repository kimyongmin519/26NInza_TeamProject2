using Member.KYM.Scripts.CombatSystems.EquipmentSystem;
using UnityEngine;

namespace Member.KYM.Scripts.Players.RobotArm
{
    public class RobotArmEquipmentController : MonoBehaviour, IPlayerEquipable
    {
        [Header("장착 대상")]
        [SerializeField] private GameObject robotArm;
        [SerializeField] private RobotArmGrabber grabber;
        [SerializeField] private RobotArmGrappler grappler;

        [Header("시작 설정")]
        [SerializeField] private bool equippedOnStart;

        public bool IsEquipped => robotArm != null && robotArm.activeSelf;

        private void Awake()
        {
            Initialize(GetComponent<PlayerController>());
            SetEquipped(equippedOnStart);
        }

        public void Initialize(PlayerController player)
        {
            if (player == null)
                return;

            if (grabber == null)
                grabber = player.GetComponentInChildren<RobotArmGrabber>(true);

            if (grappler == null)
                grappler = player.GetComponentInChildren<RobotArmGrappler>(true);

            if (robotArm == null)
            {
                if (grabber != null)
                    robotArm = grabber.gameObject;
                else if (grappler != null)
                    robotArm = grappler.gameObject;
            }
        }

        public void Equip()
        {
            SetEquipped(true);
        }

        public void Unequip()
        {
            SetEquipped(false);
        }

        public void ToggleEquipped()
        {
            SetEquipped(!IsEquipped);
        }

        public void SetEquipped(bool equipped)
        {
            if (robotArm == null || IsEquipped == equipped)
                return;

            if (!equipped)
            {
                grappler?.StopGrapple();
                grabber?.Release();
            }

            robotArm.SetActive(equipped);
        }
    }
}
