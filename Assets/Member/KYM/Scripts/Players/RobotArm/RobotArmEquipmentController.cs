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
        [SerializeField] private bool keepActiveWhenUnequipped;

        private SpriteRenderer[] _armRenderers;
        private Color[] _armColors;
        private RobotArm _armController;
        private RobotArmActionController _actionController;
        private RobotArmTakeOutController _takeOutController;
        private bool _isEquipped;

        public bool IsEquipped => robotArm != null &&
            (keepActiveWhenUnequipped ? _isEquipped : robotArm.activeSelf);

        private void Awake()
        {
            Initialize(GetComponent<PlayerController>());
            if (keepActiveWhenUnequipped && robotArm != null)
                PrepareActiveArm();
            SetEquipped(equippedOnStart);
        }

        private void PrepareActiveArm()
        {
            robotArm.SetActive(true);
            _armRenderers = robotArm.GetComponentsInChildren<SpriteRenderer>(true);
            _armColors = new Color[_armRenderers.Length];
            for (int i = 0; i < _armRenderers.Length; i++)
                _armColors[i] = _armRenderers[i].color;

            _armController = robotArm.GetComponent<RobotArm>();
            _actionController = robotArm.GetComponent<RobotArmActionController>();
            _takeOutController = robotArm.GetComponentInChildren<RobotArmTakeOutController>(true);
            _isEquipped = true;
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

            if (!keepActiveWhenUnequipped)
            {
                robotArm.SetActive(equipped);
                return;
            }

            for (int i = 0; i < _armRenderers.Length; i++)
            {
                Color color = _armColors[i];
                if (!equipped)
                    color.a = 0f;
                _armRenderers[i].color = color;
            }

            if (_armController != null)
                _armController.enabled = equipped;
            if (_actionController != null)
                _actionController.enabled = equipped;
            if (_takeOutController != null)
                _takeOutController.enabled = equipped;

            _isEquipped = equipped;
        }
    }
}
