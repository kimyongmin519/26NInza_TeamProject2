using UnityEngine;

namespace Member.KYM.Scripts.Players.RobotArm
{
    public readonly struct ThrowData
    {
        public readonly Vector2 Direction;
        public readonly GameObject Owner;
        public readonly float ArmThrowSpeed;

        public ThrowData(Vector2 direction, GameObject owner, float armThrowSpeed)
        {
            Direction = direction.normalized;
            Owner = owner;
            ArmThrowSpeed = armThrowSpeed;
        }
    }
}