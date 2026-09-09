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
            if (!GrabbableLayer.TryApply(gameObject))
            {
                Debug.LogError(
                    $"프로젝트에 {GrabbableLayer.Name} 레이어가 없습니다.",
                    this);
            }
        }

        public void OnGrappleStarted(GameObject owner)
        {
        }

        public void OnGrappleEnded(GameObject owner)
        {
        }

        private void OnValidate()
        {
            GrabbableLayer.TryApply(gameObject);
        }
    }
}
