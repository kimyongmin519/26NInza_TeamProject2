using System;
using System.Collections.Generic;
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
        [SerializeReference] public BlackboardVariable<float> GroundedVerticalTolerance = new(0.75f);
        [SerializeReference] public BlackboardVariable<float> NoProgressDuration = new(0.75f);
        [SerializeReference] public BlackboardVariable<float> MaxDuration = new(5f);
        [SerializeReference] public BlackboardVariable<int> MaxRecoveryJumps = new(2);
        [SerializeReference] public BlackboardVariable<AnimParamSO> JumpAnimation;
        [SerializeReference] public BlackboardVariable<AnimParamSO> MoveAnimation;

        private IMover _mover;
        private IAnimateRenderer _renderer;
        private ISplineMover _splineMover;
        private SplinePath _path;
        private float _targetT;
        private float _bodyToFeetOffsetY;
        private float _stuckTimer;
        private int _recoveryJumpCount;
        private bool _canJump;
        private bool _isJumping;
        private readonly HashSet<SplinePath> _attemptedPaths = new();
        private readonly MovementProgressWatchdog _progressWatchdog = new();

        protected override Status OnStart()
        {
            if (Enemy?.Value == null || Path?.Value == null)
                return Status.Failure;

            _mover = Enemy.Value.Mover;
            _renderer = Enemy.Value.Renderer;
            _splineMover = Enemy.Value.GetModule<ISplineMover>();
            if (_mover == null || _splineMover == null)
                return Status.Failure;

            _bodyToFeetOffsetY = SplineAgentPositionUtility.GetBodyToFeetOffsetY(
                _mover.RigidBody);
            Vector2 feetPosition = SplineAgentPositionUtility.GetFeetPosition(
                _mover.RigidBody,
                _bodyToFeetOffsetY);
            _attemptedPaths.Clear();
            SetTargetPath(Path.Value, feetPosition);
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
            float groundedVerticalTolerance = Mathf.Max(
                verticalTolerance,
                GroundedVerticalTolerance?.Value ?? 0.75f);
            bool isGroundedCloseEnough =
                isCloseX &&
                _mover.IsGrounded &&
                Mathf.Abs(delta.y) <= groundedVerticalTolerance;
            if ((isCloseX && isCloseY) || isGroundedCloseEnough)
                return Status.Success;

            MovementProgressState progressState = _progressWatchdog.Update(
                delta.magnitude,
                Time.deltaTime,
                _mover.IsGrounded,
                Mathf.Max(0.1f, NoProgressDuration?.Value ?? 0.75f),
                Mathf.Max(0.1f, MaxDuration?.Value ?? 5f));
            if (progressState == MovementProgressState.TimedOut)
                return TryChangePath(feetPosition) ? Status.Running : Status.Failure;

            _mover.SetMovementX(isCloseX ? 0f : Mathf.Sign(delta.x));

            bool shouldJumpToHigherPath = delta.y > verticalTolerance;
            bool shouldJumpBecauseStuck = UpdateStuckTimer(isCloseX);
            if (shouldJumpToHigherPath && _canJump && _mover.IsGrounded)
            {
                Jump();
            }
            else if (shouldJumpBecauseStuck && _canJump && _mover.IsGrounded)
            {
                int maxRecoveryJumps = Mathf.Max(
                    0,
                    MaxRecoveryJumps?.Value ?? 2);
                if (_recoveryJumpCount >= maxRecoveryJumps)
                    return TryChangePath(feetPosition) ? Status.Running : Status.Failure;

                _recoveryJumpCount++;
                _progressWatchdog.ResetStall(delta.magnitude);
                Jump();
            }
            else if (delta.y < -verticalTolerance && isCloseX && _mover.IsGrounded)
            {
                if (_mover.TryDropThroughPlatform())
                    _progressWatchdog.ResetStall(delta.magnitude);
                else if (progressState == MovementProgressState.Stalled)
                    return TryChangePath(feetPosition) ? Status.Running : Status.Failure;
            }
            else if (progressState == MovementProgressState.Stalled &&
                     _mover.IsGrounded)
            {
                int maxRecoveryJumps = Mathf.Max(
                    0,
                    MaxRecoveryJumps?.Value ?? 2);
                if (_recoveryJumpCount >= maxRecoveryJumps)
                    return TryChangePath(feetPosition) ? Status.Running : Status.Failure;

                _recoveryJumpCount++;
                _progressWatchdog.ResetStall(delta.magnitude);
                Jump();
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
            _recoveryJumpCount = 0;
            _isJumping = false;
            _attemptedPaths.Clear();
        }

        private bool TryChangePath(Vector2 feetPosition)
        {
            SplinePath[] paths = _splineMover?.SplinePaths?.Paths;
            if (paths == null || paths.Length == 0)
                return false;

            SplinePath closestPath = null;
            float closestSqrDistance = float.PositiveInfinity;

            foreach (SplinePath candidate in paths)
            {
                if (candidate == null ||
                    !candidate.isActiveAndEnabled ||
                    _attemptedPaths.Contains(candidate))
                {
                    continue;
                }

                float candidateT = candidate.GetClosestEndT(feetPosition);
                Vector2 candidatePosition = candidate.EvaluatePosition(candidateT);
                float sqrDistance = (candidatePosition - feetPosition).sqrMagnitude;
                if (sqrDistance >= closestSqrDistance)
                    continue;

                closestSqrDistance = sqrDistance;
                closestPath = candidate;
            }

            if (closestPath == null)
                return false;

            Path.Value = closestPath;
            SetTargetPath(closestPath, feetPosition);
            return true;
        }

        private void SetTargetPath(SplinePath path, Vector2 feetPosition)
        {
            _path = path;
            _targetT = _path.GetClosestEndT(feetPosition);
            _attemptedPaths.Add(_path);
            _stuckTimer = 0f;
            _recoveryJumpCount = 0;
            _progressWatchdog.Reset(
                Vector2.Distance(_path.EvaluatePosition(_targetT), feetPosition));
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
