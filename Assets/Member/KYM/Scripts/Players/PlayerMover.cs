using System;
using KimLIb.ModuleSystems;
using Member.KYM.Scripts.Agents;
using UnityEngine;

namespace Member.KYM.Scripts.Players
{
    public class PlayerMover : MonoBehaviour, IMover, IModule
    {
        [SerializeField] private float moveSpeed;
        [Header("땅 체크")]
        [SerializeField] private LayerMask whatIsGround; //차후 센서시스템으로 변경
        [SerializeField] private Vector2 groundCheckSize;
        [Header("외부 힘")]
        [SerializeField, Min(0f)] private float externalVelocityDamping = 20f;
        
        public Rigidbody2D RigidBody { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool CanManualMovement { get; set; } = true;
        public event Action<bool> OnGroundStatusChange;
        public event Action<Vector2> OnVelocityChange;

        private float _moveDirX;
        private ModuleOwner _owner;
        private float _originalGravityScale;
        private float _externalVelocityX;
        private float _lastAppliedExternalVelocityX;
        
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            RigidBody = owner.GetComponent<Rigidbody2D>();
            _originalGravityScale = RigidBody.gravityScale;
        }
        
        public void SetMoveSpeedMultiplier(float value)
        {
            
        }

        public void SetGravityScale(float value) => RigidBody.gravityScale = _originalGravityScale * value;

        public void AddForceToAgent(Vector2 force)
        {
            if (!Mathf.Approximately(force.x, 0f))
                _externalVelocityX += force.x / Mathf.Max(RigidBody.mass, Mathf.Epsilon);

            if (!Mathf.Approximately(force.y, 0f))
                RigidBody.AddForce(Vector2.up * force.y, ForceMode2D.Impulse);
        }

        private void FixedUpdate()
        {
            MoveCharacter();
            CheckGround();
        }

        private void MoveCharacter()
        {
            float velocityWithoutExternalForce =
                RigidBody.linearVelocityX - _lastAppliedExternalVelocityX;

            if (CanManualMovement)
                velocityWithoutExternalForce = _moveDirX * moveSpeed;

            RigidBody.linearVelocityX = velocityWithoutExternalForce + _externalVelocityX;
            _lastAppliedExternalVelocityX = _externalVelocityX;
            _externalVelocityX = Mathf.MoveTowards(
                _externalVelocityX,
                0f,
                externalVelocityDamping * Time.fixedDeltaTime);

            OnVelocityChange?.Invoke(RigidBody.linearVelocity);
        }
        
        private void CheckGround()
        {
            bool before = IsGrounded;
            IsGrounded = Physics2D.OverlapBox(transform.position, groundCheckSize, 0, whatIsGround);
            
            if(before != IsGrounded)
                OnGroundStatusChange?.Invoke(IsGrounded);
        }

        public void StopImmediately(bool xAxis, bool yAxis)
        {
            if (xAxis)
            {
                _externalVelocityX = 0f;
                _lastAppliedExternalVelocityX = 0f;
                RigidBody.linearVelocityX = 0;
            }
            if (yAxis)
                RigidBody.linearVelocityY = 0;
        }

        public void SetMovementX(float value) => _moveDirX = value;
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, groundCheckSize);
        }

        public bool TryDropThroughPlatform()
        {
            throw new NotImplementedException();
        }
    }
}
