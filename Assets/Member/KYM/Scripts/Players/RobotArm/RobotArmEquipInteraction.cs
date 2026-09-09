using Member.KYM.Scripts.CoreSystems.InteractSystems;
using UnityEngine;

namespace Member.KYM.Scripts.Players.RobotArm
{
    [RequireComponent(typeof(PlayerInteractor))]
    public class RobotArmEquipInteraction : MonoBehaviour
    {
        private PlayerInteractor _interactor;

        private void Awake()
        {
            _interactor = GetComponent<PlayerInteractor>();
            _interactor.OnPlayerSuccess.AddListener(HandleInteractionSuccess);
        }

        private void OnDestroy()
        {
            if (_interactor != null)
            {
                _interactor.OnPlayerSuccess.RemoveListener(
                    HandleInteractionSuccess
                );
            }
        }

        private void HandleInteractionSuccess(PlayerController player)
        {
            if (player == null)
                return;

            RobotArmEquipmentController equipmentController =
                player.GetComponent<RobotArmEquipmentController>();

            equipmentController?.Equip();
        }
    }
}
