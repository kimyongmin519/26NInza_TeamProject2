using UnityEngine;

namespace Member.KYM.Scripts.RobotArm
{
    public interface IGrabbable
    {
        bool CanBeGrabbed { get; }
        Transform GrabTransform { get; }

        void Grab(Transform grabPoint, GameObject grabber);
        void Release();
        void Throw(Vector2 velocity, GameObject newOwner);
    }
}
