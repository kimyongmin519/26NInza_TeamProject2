using System;
using KimLIb.ModuleSystems;
using Member.KYM.Scripts.Agents;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    public class LostSoulPresentation : MonoBehaviour, IModule, IAnimateRenderer, IMover
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody2D rigidBody;

        public Animator Animator => animator;
        public float FacingDirection { get; private set; } = 1f;
        public bool IsGrounded => false;
        public bool CanManualMovement { get; set; }
        public Rigidbody2D RigidBody => rigidBody;
        public event Action<bool> OnGroundStatusChange;
        public event Action<Vector2> OnVelocityChange;

        public void Initialize(ModuleOwner owner)
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (rigidBody == null) rigidBody = owner.GetComponent<Rigidbody2D>();
        }

        public void PlayClip(int clipHash)
        {
            if (animator != null && animator.HasState(0, clipHash))
                animator.CrossFade(clipHash, 0.04f, 0, 0f);
        }

        public void FlipController(float xMoveDirection)
        {
            if (Mathf.Approximately(xMoveDirection, 0f)) return;
            FacingDirection = Mathf.Sign(xMoveDirection);
        }

        public void SetMoveSpeedMultiplier(float value) { }
        public void SetGravityScale(float value) { }
        public void SetMovementX(float value) { }
        public bool TryDropThroughPlatform() => false;

        public void AddForceToAgent(Vector2 force)
        {
            if (rigidBody != null) rigidBody.AddForce(force, ForceMode2D.Impulse);
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
    }
}
