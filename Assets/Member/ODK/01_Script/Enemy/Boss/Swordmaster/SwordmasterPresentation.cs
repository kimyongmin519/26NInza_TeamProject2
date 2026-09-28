using System;
using KimLIb.ModuleSystems;
using Member.KYM.Scripts.Agents;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Swordmaster
{
    [DisallowMultipleComponent]
    public sealed class SwordmasterPresentation : MonoBehaviour,
        IModule,
        IAnimateRenderer,
        IMover
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody2D rigidBody;

        public Animator Animator => animator;
        public Rigidbody2D RigidBody => rigidBody;
        public float FacingDirection { get; private set; } = 1f;
        public bool IsGrounded => true;
        public bool CanManualMovement { get; set; }

        public event Action<bool> OnGroundStatusChange;
        public event Action<Vector2> OnVelocityChange;

        public void Initialize(ModuleOwner owner)
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);
            if (rigidBody == null)
                rigidBody = owner.GetComponent<Rigidbody2D>();

            OnGroundStatusChange?.Invoke(true);
            OnVelocityChange?.Invoke(
                rigidBody != null ? rigidBody.linearVelocity : Vector2.zero
            );
        }

        public void PlayClip(int clipHash)
        {
            if (animator != null && animator.HasState(0, clipHash))
                animator.CrossFadeInFixedTime(clipHash, 0.04f, 0, 0f);
        }

        public void FlipController(float xMoveDirection)
        {
            if (!Mathf.Approximately(xMoveDirection, 0f))
                FacingDirection = Mathf.Sign(xMoveDirection);
        }

        public void SetMoveSpeedMultiplier(float value) { }

        public void SetGravityScale(float value)
        {
            if (rigidBody != null)
                rigidBody.gravityScale = value;
        }

        public void AddForceToAgent(Vector2 force)
        {
            if (rigidBody == null) return;
            rigidBody.AddForce(force, ForceMode2D.Impulse);
            OnVelocityChange?.Invoke(rigidBody.linearVelocity);
        }

        public void StopImmediately(bool xAxis, bool yAxis)
        {
            if (rigidBody == null) return;
            Vector2 velocity = rigidBody.linearVelocity;
            if (xAxis) velocity.x = 0f;
            if (yAxis) velocity.y = 0f;
            rigidBody.linearVelocity = velocity;
            OnVelocityChange?.Invoke(velocity);
        }

        public void SetMovementX(float value)
        {
            if (rigidBody == null) return;
            rigidBody.linearVelocityX = value;
            OnVelocityChange?.Invoke(rigidBody.linearVelocity);
        }

        public bool TryDropThroughPlatform() => false;
    }
}
