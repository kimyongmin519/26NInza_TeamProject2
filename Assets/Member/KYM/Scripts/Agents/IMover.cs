using System;
using UnityEngine;

namespace Member.KYM.Scripts.Agents
{
    public interface IMover
    {
        bool IsGrounded { get; }
        bool CanManualMovement { get; set; }
        event Action<bool> OnGroundStatusChange;
        event Action<Vector2> OnVelocityChange;
        Rigidbody2D RigidBody { get; }
        void SetMoveSpeedMultiplier(float value);
        void SetGravityScale(float value);
        void AddForceToAgent(Vector2 force);
        void StopImmediately(bool xAxis, bool yAxis);
        void SetMovementX(float value);
        bool TryDropThroughPlatform();
        
    }
}
