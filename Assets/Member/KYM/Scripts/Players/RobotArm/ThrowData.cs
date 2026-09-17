using KimLIb.ModuleSystems;
using UnityEngine;

namespace Member.KYM.Scripts.Players.RobotArm
{
    public readonly struct ThrowData
    {
        public readonly Vector2 Direction;
        public readonly ModuleOwner Owner;
        public readonly float ArmThrowSpeed;

        public ThrowData(Vector2 direction, ModuleOwner owner, float armThrowSpeed)
        {
            Direction = direction.normalized;
            Owner = owner;
            ArmThrowSpeed = armThrowSpeed;
        }
    }
}
