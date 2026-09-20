using System;
using KimLIb.AnimatorSystems;
using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.Enemies.Boss.Splines;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Member.KYM.Scripts.Enemies.Boss.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "MoveToSplinePathStart", story: "[Enemy] moves to closest end of [Path]", category: "Action", id: "20327c0b29eb4137918047ce56570e73")]
    public partial class MoveToSplinePathStartAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractBoss> Enemy;
        [SerializeReference] public BlackboardVariable<SplinePath> Path;
        [SerializeReference] public BlackboardVariable<float> HorizontalTolerance = new(0.15f);
        [SerializeReference] public BlackboardVariable<float> VerticalTolerance = new(0.15f);
        [SerializeReference] public BlackboardVariable<float> JumpForce = new(9f);
        [SerializeReference] public BlackboardVariable<float> StuckVelocityThreshold = new(0.1f);
        [SerializeReference] public BlackboardVariable<float> StuckJumpDelay = new(0.5f);
        [SerializeReference] public BlackboardVariable<AnimParamSO> JumpAnimation;
        [SerializeReference] public BlackboardVariable<AnimParamSO> MoveAnimation;

        private IMover _mover;
        private IAnimateRenderer _renderer;
        private SplinePath _path;
        private float _targetT;
        private float _bodyToFeetOffsetY;
        private float _stuckTimer;
        private bool _canJump;
        private bool _isJumping;

        protected override Status OnStart()
        {
            if (Enemy?.Value == null || Path?.Value == null)
                return Status.Failure;

            _mover = Enemy.Value.Mover;
            _renderer = Enemy.Value.Renderer;
            if (_mover == null)
                return Status.Failure;

            _path = Path.Value;
            _bodyToFeetOffsetY = SplineAgentPositionUtility.GetBodyToFeetOffsetY(
                _mover.RigidBody);
            Vector2 feetPosition = SplineAgentPositionUtility.GetFeetPosition(
                _mover.RigidBody,
                _bodyToFeetOffsetY);
            _targetT = _path.GetClosestEndT(feetPosition);
            _stuckTimer = 0f;
            _canJump = _mover.IsGrounded;
            _mover.OnGroundStatusChange += HandleGroundStatusChange;

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (_mover == null || _path == null || _mover.RigidBody == null)
                return Status.Failure;

            Vector2 targetPosition = _path.EvaluatePosition(_targetT);
            Vector2 feetPosition = SplineAgentPositionUtility.GetFeetPosition(
                _mover.RigidBody,
                _bodyToFeetOffsetY);
            Vector2 delta = targetPosition - feetPosition;
            float horizontalTolerance = Mathf.Max(0f, HorizontalTolerance?.Value ?? 0f);
            float verticalTolerance = Mathf.Max(0f, VerticalTolerance?.Value ?? 0f);

            bool isCloseX = Mathf.Abs(delta.x) <= horizontalTolerance;
            bool isCloseY = Mathf.Abs(delta.y) <= verticalTolerance;
            if (isCloseX && isCloseY)
                return Status.Success;

            _mover.SetMovementX(isCloseX ? 0f : Mathf.Sign(delta.x));

            bool shouldJumpToHigherPath = delta.y > verticalTolerance;
            bool shouldJumpBecauseStuck = UpdateStuckTimer(isCloseX);
            if ((shouldJumpToHigherPath || shouldJumpBecauseStuck) &&
                _canJump &&
                _mover.IsGrounded)
            {
                Jump();
            }
            else if (delta.y < -verticalTolerance && isCloseX && _mover.IsGrounded)
            {
                _mover.TryDropThroughPlatform();
            }

            return Status.Running;
        }

        protected override void OnEnd()
        {
            if (_mover == null)
                return;

            _mover.SetMovementX(0f);
            _mover.OnGroundStatusChange -= HandleGroundStatusChange;
            _stuckTimer = 0f;
            _isJumping = false;
        }

        private bool UpdateStuckTimer(bool isCloseX)
        {
            float velocityThreshold = Mathf.Max(
                0f,
                StuckVelocityThreshold?.Value ?? 0f);
            bool isNearlyStopped =
                Mathf.Abs(_mover.RigidBody.linearVelocityX) <= velocityThreshold;

            if (isCloseX || !_mover.IsGrounded || !isNearlyStopped)
            {
                _stuckTimer = 0f;
                return false;
            }

            _stuckTimer += Time.deltaTime;
            float jumpDelay = Mathf.Max(0f, StuckJumpDelay?.Value ?? 0f);
            return _stuckTimer >= jumpDelay;
        }

        private void Jump()
        {
            float jumpForce = Mathf.Max(0f, JumpForce?.Value ?? 0f);
            _mover.AddForceToAgent(Vector2.up * jumpForce);
            PlayAnimation(JumpAnimation);

            _stuckTimer = 0f;
            _canJump = false;
            _isJumping = true;
        }

        private void HandleGroundStatusChange(bool isGrounded)
        {
            if (!isGrounded)
                return;

            _canJump = true;
            if (!_isJumping)
                return;

            _isJumping = false;
            PlayAnimation(MoveAnimation);
        }

        private void PlayAnimation(BlackboardVariable<AnimParamSO> animation)
        {
            if (_renderer == null || animation?.Value == null)
                return;

            _renderer.PlayClip(animation.Value.ParamHash);
        }
    }
}
