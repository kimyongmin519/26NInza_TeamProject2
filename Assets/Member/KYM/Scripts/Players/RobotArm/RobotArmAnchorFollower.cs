using UnityEngine;

namespace Member.KYM.Scripts.Players.RobotArm
{
    [DefaultExecutionOrder(-100)]
    public class RobotArmAnchorFollower : MonoBehaviour
    {
        [Header("추적 설정")]
        [SerializeField] private Transform anchor;
        [SerializeField] private Transform robotArm;
        [SerializeField] private Vector3 localOffset;

        private void LateUpdate()
        {
            if (anchor == null || robotArm == null)
                return;

            robotArm.position = anchor.TransformPoint(localOffset);
        }
    }
}
