using KimLIb.ModuleSystems;
using Member.KYM.Scripts.Agents;
using System;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Modules
{
    public class FlyingMover : MonoBehaviour, IModule, IMover
    {
        //юс╫ц
        [SerializeField] private float MoveSpeed;

        public float MoveSpeedMultiplier { get; private set; } = 1f;
        public bool IsGrounded => false;

        public bool CanManualMovement { get; set; }

        public Rigidbody2D RigidBody { get; private set; }

        public event Action<bool> OnGroundStatusChange;
        public event Action<Vector2> OnVelocityChange;
        private ModuleOwner owner;

        private Vector2 prevForce =Vector2.zero;
        public void Initialize(ModuleOwner owner)
        {
            this.owner = owner;
            RigidBody = owner.GetComponent<Rigidbody2D>();
            RigidBody.gravityScale = 0f;
        }

        public void AddForceToAgent(Vector2 force)
        {
            if (prevForce != force)
            {
                RigidBody.AddForce(force * MoveSpeed);
                prevForce = force;
            }
            
        }
        public void SetForceToAgent(Vector2 force)
        {
            if (prevForce != force)
            {
                RigidBody.linearVelocity = force * MoveSpeed;
                prevForce = force;
            }

        }
        public void SetGravityScale(float value)
        {
            RigidBody.gravityScale = value;
        }

        public void SetMovementX(float value)
        {
            RigidBody.linearVelocityX = value;
        }


        public void SetMoveSpeedMultiplier(float value)
        {
            MoveSpeedMultiplier = value;
        }

        public void StopImmediately(bool xAxis, bool yAxis)
        {
            if (xAxis)
                RigidBody.linearVelocityX = 0;
            if (yAxis)
                RigidBody.linearVelocityY = 0;
        }
    }

}
