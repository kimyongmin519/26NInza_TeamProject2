using Member.KYM.Scripts.Agents.FSM;
using Member.KYM.Scripts.Players.RobotArm;
using UnityEngine;

namespace Member.KYM.Scripts.Players.Equipment
{
    public class RobotArmEquipment : MonoBehaviour, IPlayerEquipment
    {
        [SerializeField] private EquipmentDefinitionSO definition;
        [SerializeField] private RobotArmGrabber grabber;
        [SerializeField] private RobotArmGrappler grappler;

        public EquipmentDefinitionSO Definition => definition;
        public GameObject EquipmentObject => gameObject;
        public bool IsEquipped { get; private set; }

        private PlayerController _player;
        private bool _eventsBound;
        private bool _ownsPlayerAction;

        public void Initialize(PlayerController player)
        {
            ResolveReferences();

            if (_player == player && _eventsBound)
                return;

            UnbindEvents();
            _player = player;
            BindEvents();
        }

        public void Equip()
        {
            ResolveReferences();
            BindEvents();
            IsEquipped = true;
            gameObject.SetActive(true);
        }

        public void Unequip()
        {
            if (grappler != null && grappler.IsGrappling)
                grappler.StopGrapple();

            grabber?.Release();
            IsEquipped = false;
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (!IsEquipped)
                return;

            if (grappler != null && grappler.IsGrappling)
                grappler.StopGrapple();

            grabber?.Release();
            IsEquipped = false;
        }

        private void OnDestroy()
        {
            UnbindEvents();
        }

        private void ResolveReferences()
        {
            if (grabber == null)
                grabber = GetComponent<RobotArmGrabber>();

            if (grappler == null)
                grappler = GetComponent<RobotArmGrappler>();
        }

        private void BindEvents()
        {
            if (_eventsBound || grappler == null)
                return;

            grappler.GrappleStarted += HandleGrappleStarted;
            grappler.GrappleEnded += HandleGrappleEnded;
            _eventsBound = true;
        }

        private void UnbindEvents()
        {
            if (!_eventsBound || grappler == null)
                return;

            grappler.GrappleStarted -= HandleGrappleStarted;
            grappler.GrappleEnded -= HandleGrappleEnded;
            _eventsBound = false;
        }

        private void HandleGrappleStarted()
        {
            if (!IsEquipped || _player == null)
                return;

            _ownsPlayerAction = true;
            _player.BeginEquipmentAction(PlayerStateEnum.GRAPPLE);
        }

        private void HandleGrappleEnded()
        {
            if (!_ownsPlayerAction)
                return;

            _ownsPlayerAction = false;
            _player?.EndEquipmentAction();
        }
    }
}
