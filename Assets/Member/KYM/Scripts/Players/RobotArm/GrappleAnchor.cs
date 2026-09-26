using UnityEngine;

namespace Member.KYM.Scripts.Players.RobotArm
{
    public class GrappleAnchor : MonoBehaviour, IGrappleAnchor
    {
        [Header("그래플 설정")]
        [SerializeField] private Transform grapplePoint;
        [SerializeField] private bool canGrapple = true;

        public bool CanGrapple => canGrapple;
        public Transform GrapplePoint =>
            grapplePoint != null ? grapplePoint : transform;

        private void Awake()
        {
            GrabbableLayer.Validate(gameObject);
        }

        public void OnGrappleStarted(GameObject owner)
        {
        }

        public void OnGrappleEnded(GameObject owner)
        {
        }

    }
}
