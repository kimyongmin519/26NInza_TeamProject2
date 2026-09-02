using System;
using KimLIb.ModuleSystems;
using Member.KYM.Scripts.Agents;
using UnityEngine;

namespace Member.KYM.Scripts.Players
{
    public class PlayerMover : MonoBehaviour, IMover, IModule
    {
        [SerializeField] private float moveSpeed;
        public Rigidbody2D RigidBody2D { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool CanManualMovement { get; set; } = true;
        public event Action<bool> OnGroundStatusChange;
        public event Action<Vector2> OnVelocityChange;

        private float _moveDirX;
        private ModuleOwner _owner;
        
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            RigidBody2D = owner.GetComponent<Rigidbody2D>();
        }
        
        public void SetMoveSpeedMultiplier(float value)
        {
            
        }

        public void SetGravityScale(float value)
        {
            
        }

        public void AddForceToAgent(Vector2 force)
        {
            
        }

        private void FixedUpdate()
        {
            RigidBody2D.linearVelocityX = _moveDirX * moveSpeed;
        }

        public void StopImmediately(bool xAxis, bool yAxis)
        {
            if (xAxis)
                RigidBody2D.linearVelocityX = 0;
            if (yAxis)
                RigidBody2D.linearVelocityY = 0;
        }

        public void SetMovementX(float value) => _moveDirX = value;
        
    }
}