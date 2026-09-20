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
    [NodeDescription(
        name: "MoveToClosestSplinePathStart",
        story: "[Enemy] moves to closest spline path start",
        category: "Action",
        id: "b41584714ad04e95a40586a5d07e0195")]
    public partial class MoveToClosestSplinePathStartAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractBoss> Enemy;
        [SerializeReference] public BlackboardVariable<float> HorizontalTolerance = new(0.15f);
        [SerializeReference] public BlackboardVariable<float> VerticalTolerance = new(0.15f);
        [SerializeReference] public BlackboardVariable<float> JumpForce = new(9f);
        [SerializeReference] public BlackboardVariable<AnimParamSO> JumpAnimation;
        [SerializeReference] public BlackboardVariable<AnimParamSO> MoveAnimation;

        private IMover _mover;
        private IAnimateRenderer _renderer;
        private SplinePath _path;
        private float _targetT;
        private float _bodyToFeetOffsetY;
        private bool _canJump;
        private bool _isJumping;

        protected override Status OnStart()
        {
            if (Enemy?.Value == null)
                return Status.Failure;

            _mover = Enemy.Value.Mover;
            _renderer = Enemy.Value.Renderer;
            ISplineMover splineMover = Enemy.Value.GetModule<ISplineMover>();
            if (_mover == null || splineMover == null)
                return Status.Failure;

            if (!splineMover.TryGetClosestPath(
                    out _path,
                    out _,
                    true))
            {
                return Status.Failure;
            }

            _bodyToFeetOffsetY = SplineAgentPositionUtility.GetBodyToFeetOffsetY(
                _mover.RigidBody);
            Vector2 feetPosition = SplineAgentPositionUtility.GetFeetPosition(
                _mover.RigidBody,
                _bodyToFeetOffsetY);
            _targetT = _path.GetClosestEndT(feetPosition);
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

            if (delta.y > verticalTolerance && _canJump && _mover.IsGrounded)
            {
                float jumpForce = Mathf.Max(0f, JumpForce?.Value ?? 0f);
                _mover.AddForceToAgent(Vector2.up * jumpForce);
                PlayAnimation(JumpAnimation);
                _canJump = false;
                _isJumping = true;
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
            _isJumping = false;
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
