using System;
using System.Collections;
using KimLIb.ModuleSystems;
using Member.KYM.Scripts.Agents;
using UnityEngine;

namespace Member.KYM.Scripts.Players
{
    public class AgentMover : MonoBehaviour, IMover, IModule
    {
        [SerializeField] private float moveSpeed;
        [Header("일방향 플랫폼 통과")]
        [SerializeField, Min(0f)] private float dropThroughSpeed = 2f;
        [SerializeField, Min(0.1f)] private float dropThroughDelay = 1f;
        [Header("추가적인 중력")]
        [SerializeField] private bool useExtraGravity;
        [SerializeField] private float extraGravityDelay;
        [SerializeField] private float extraGravityPower;
        
        public Rigidbody2D RigidBody { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool CanManualMovement { get; set; } = true;
        public event Action<bool> OnGroundStatusChange;
        public event Action<Vector2> OnVelocityChange;

        private float _moveDirX;
        private ModuleOwner _owner;
        private float _originalGravityScale;
        private AgentSensor _sensor;
        
        private Collider2D _bodyCollider;
        private Collider2D _ignoredPlatformCollider;
        private bool _isDroppingThrough;

        private float _extraGravityTimer;
        
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            RigidBody = owner.GetComponent<Rigidbody2D>();
            _bodyCollider = owner.GetComponent<Collider2D>();
            _sensor = owner.GetModule<AgentSensor>();
            _originalGravityScale = RigidBody.gravityScale;

            Debug.Assert(_sensor != null, $"{owner.name}에 AgentSensor 모듈이 없습니다.");
            if (_sensor != null)
            {
                _sensor.OnGroundStatusChange += HandleGroundStatusChange;
                SetGrounded(_sensor.IsGrounded);
            }
        }
        
        public void SetMoveSpeedMultiplier(float value)
        {
            
        }

        public void SetGravityScale(float value) => RigidBody.gravityScale = _originalGravityScale * value;

        public void AddForceToAgent(Vector2 force) => RigidBody.AddForce(force, ForceMode2D.Impulse);

        private void FixedUpdate()
        {
            MoveCharacter();
            CalculateExtraGravity();
        }

        private void MoveCharacter()
        {
            if (CanManualMovement)
                RigidBody.linearVelocityX = _moveDirX * moveSpeed;

            OnVelocityChange?.Invoke(RigidBody.linearVelocity);
        }
        
        private void CalculateExtraGravity()
        {
            if (!useExtraGravity) return;
            
            if (!IsGrounded)
            {
                _extraGravityTimer += Time.fixedDeltaTime;

                if (_extraGravityTimer >= extraGravityDelay)
                {
                    RigidBody.AddForceY(-extraGravityPower, ForceMode2D.Force);
                }
            }
        }
        
        public void StopImmediately(bool xAxis, bool yAxis)
        {
            if (xAxis)
            {
                _moveDirX = 0f;
                RigidBody.linearVelocityX = 0;
            }
            if (yAxis)
                RigidBody.linearVelocityY = 0;
        }

        public void SetMovementX(float value) => _moveDirX = value;

        public bool TryDropThroughPlatform()
        {
            if (_isDroppingThrough || !IsGrounded || _bodyCollider == null || _sensor == null)
                return false;

            if (!_sensor.TryGetOneWayPlatformBelow(out Collider2D platformCollider))
                return false;

            StartCoroutine(DropThroughPlatform(platformCollider));
            return true;
        }

        private void HandleGroundStatusChange(bool isGrounded)
        {
            if (!_isDroppingThrough)
                SetGrounded(isGrounded);
        }

        private IEnumerator DropThroughPlatform(Collider2D platformCollider)
        {
            _isDroppingThrough = true;
            _ignoredPlatformCollider = platformCollider;
            Physics2D.IgnoreCollision(_bodyCollider, platformCollider, true);
            SetGrounded(false);
            RigidBody.linearVelocityY = Mathf.Min(RigidBody.linearVelocityY, -dropThroughSpeed);

            float elapsedTime = 0f;
            while (elapsedTime < dropThroughDelay &&
                   platformCollider != null &&
                   _bodyCollider.bounds.max.y >= platformCollider.bounds.min.y)
            {
                elapsedTime += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            RestorePlatformCollision();
        }

        private void SetGrounded(bool isGrounded)
        {
            if (IsGrounded == isGrounded)
                return;

            IsGrounded = isGrounded;
            OnGroundStatusChange?.Invoke(IsGrounded);
        }

        private void RestorePlatformCollision()
        {
            if (_bodyCollider != null && _ignoredPlatformCollider != null)
                Physics2D.IgnoreCollision(_bodyCollider, _ignoredPlatformCollider, false);

            _ignoredPlatformCollider = null;
            _isDroppingThrough = false;
            if (_sensor != null)
                SetGrounded(_sensor.IsGrounded);
        }

        private void OnDisable()
        {
            RestorePlatformCollision();
        }

        private void OnDestroy()
        {
            if (_sensor != null)
                _sensor.OnGroundStatusChange -= HandleGroundStatusChange;
        }
    }
}
