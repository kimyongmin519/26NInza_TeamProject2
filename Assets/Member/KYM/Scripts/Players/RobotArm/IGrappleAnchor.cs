using UnityEngine;

namespace Member.KYM.Scripts.Players.RobotArm
{
    public interface IGrappleAnchor
    {
        bool CanGrapple { get; }
        Transform GrapplePoint { get; }

        void OnGrappleStarted(GameObject owner);
        void OnGrappleEnded(GameObject owner);
    }
}