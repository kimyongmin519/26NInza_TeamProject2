using Member.KYM.Scripts.CoreSystems;
using UnityEngine;

namespace Member.KYM.Scripts.Players.RobotArm
{
    public class RobotArmActionController : MonoBehaviour
    {
        [Header("필수 참조")]
        [SerializeField] private PlayerInputSO playerInput;
        [SerializeField] private RobotArmGrabber grabber;
        [SerializeField] private RobotArmGrappler grappler;

        public bool IsSkillLocked { get; private set; }
        public bool CanUseLaser =>
            !IsSkillLocked &&
            !grabber.IsHolding &&
            !grabber.IsBusy &&
            !grappler.IsGrappling;

        private void Awake()
        {
            if (grabber == null)
                grabber = GetComponent<RobotArmGrabber>();

            if (grappler == null)
                grappler = GetComponent<RobotArmGrappler>();
        }

        private void OnEnable()
        {
            if (playerInput == null)
                return;

            playerInput.AttackPressed += HandleAttack;
            playerInput.AttackCancelPressed += HandleCancel;
        }

        private void OnDisable()
        {
            if (playerInput == null)
                return;

            playerInput.AttackPressed -= HandleAttack;
            playerInput.AttackCancelPressed -= HandleCancel;
        }

        public void SetSkillLocked(bool isLocked)
        {
            IsSkillLocked = isLocked;
        }

        private void HandleAttack()
        {
            if (grappler.IsGrappling)
            {
                grappler.StopGrapple();
                return;
            }

            if (IsSkillLocked)
                return;

            if (grabber.IsBusy)
                return;

            if (grabber.IsHolding)
            {
                grabber.TryThrow();
                return;
            }

            if (grabber.TryGrab())
                return;

            if (grappler.TryStartNearest(grabber.GrabPoint.position))
                return;

            grabber.PlayFailedAction();
        }

        private void HandleCancel()
        {
            if (grappler.IsGrappling)
            {
                grappler.StopGrapple();
                return;
            }

            grabber.Release();
        }
    }
}
