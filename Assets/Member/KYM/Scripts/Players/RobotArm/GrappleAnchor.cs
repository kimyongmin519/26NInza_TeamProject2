using UnityEngine;

namespace Member.KYM.Scripts.Players.RobotArm
{
    public class GrappleAnchor : MonoBehaviour, IGrappleAnchor
    {
        [SerializeField] private Transform grapplePoint;
        [SerializeField] private bool canGrapple = true;

        public bool CanGrapple => canGrapple;
        public Transform GrapplePoint =>
            grapplePoint != null ? grapplePoint : transform;

        public void OnGrappleStarted(GameObject owner)
        {
        }

        public void OnGrappleEnded(GameObject owner)
        {
        }
    }
}