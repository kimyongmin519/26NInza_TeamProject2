using System;
using UnityEngine;

namespace Member.KYM.Scripts.RobotArm
{
    public class ReflectableProjectile2D : GrabbableRigidbody2D, IReflectableProjectile
    {
        [field: SerializeField] public GameObject Owner { get; private set; }

        public event Action<GameObject, Vector2> Reflected;

        public void Reflect(GameObject newOwner, Vector2 velocity)
        {
            Owner = newOwner;
            Reflected?.Invoke(newOwner, velocity);
        }

        public override void Throw(Vector2 velocity, GameObject newOwner)
        {
            if (!IsHeld)
                return;

            base.Throw(velocity, newOwner);
            Reflect(newOwner, velocity);
        }
    }
}
