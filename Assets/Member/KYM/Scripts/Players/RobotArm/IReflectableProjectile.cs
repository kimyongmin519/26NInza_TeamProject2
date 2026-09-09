using UnityEngine;

namespace Member.KYM.Scripts.Players.RobotArm
{
    public interface IReflectableProjectile : IGrabbable
    {
        GameObject Owner { get; }
        void Reflect(GameObject newOwner, Vector2 velocity);
    }
}
