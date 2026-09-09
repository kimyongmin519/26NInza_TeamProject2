using System;
using UnityEngine;

namespace Member.KYM.Scripts.Players.RobotArm
{
    public class ReflectableProjectile : GrabbableRigidbody, IReflectableProjectile
    {
        [SerializeField] private float testSpeed;
        [field: SerializeField] public GameObject Owner { get; private set; }

        public event Action<GameObject, Vector2> Reflected;

        public void Reflect(GameObject newOwner, Vector2 velocity)
        {
            Owner = newOwner;
            Reflected?.Invoke(newOwner, velocity);
        }

        public override void Throw(ThrowData throwData)
        {
            /*if (!IsHeld)
                return;

            RestorePhysicsState();

            Owner = throwData.Owner;
            Rigidbody.linearVelocity = throwData.Direction * testSpeed;

            projectile.SetOwner(context.Owner);
            projectile.ApplyData(projectileData);*/
        }
    }
}
