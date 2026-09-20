using KimLIb.AnimatorSystems;
using KimLIb.ModuleSystems;
using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.Enemies.Boss.Splines.SplineEvents;
using Reflex.Attributes;
using UnityEngine;

namespace Member.KYM.Scripts.Enemies.Boss.Splines
{
    public class BossSplineMover : MonoBehaviour, ISplineMover, IModule, IAfterInitModule
    {
        private enum MoveState
        {
            Idle,
            FollowingSpline,
            Jumping
        }

        [Header("스플라인 이동")]
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [Inject] [field: SerializeField] public SplinePaths SplinePaths { get; private set; }

        [SerializeField] private SplinePath currentPath;

        private AbstractBoss _boss;
        private IMover _baseMover;
        private Rigidbody2D _rigidBody;

        private MoveState _state;
        private bool _isCompleted;
        private float _currentT;
        private float _bodyToFeetOffsetY;
        private float _speedMultiplier = 1f;
        private int _direction = 1;
        private bool _useGravity;

        private bool _previousCanManualMovement;
        private float _previousGravityScale;

        private SplineJumpSettings _jumpSettings;
        private float _jumpTargetT;
        private float _jumpElapsedTime;
        private Vector2 _jumpStartFeetPosition;

        public bool IsFollowing => _state != MoveState.Idle;
        public bool IsCompleted => _isCompleted;
        public SplinePath LastCompletedPath { get; private set; }

        public void Initialize(ModuleOwner owner)
        {
            _boss = owner as AbstractBoss;
            Debug.Assert(_boss != null, "_boss is null");

            _baseMover = owner.GetModule<IMover>();
        }

        public void AfterInit()
        {
            if (_baseMover == null)
                return;

            _rigidBody = _baseMover.RigidBody;
            UpdateBodyToFeetOffset();
        }
        
        private void FixedUpdate()
        {
            if (currentPath == null || _rigidBody == null)
                return;

            switch (_state)
            {
                case MoveState.FollowingSpline:
                    UpdateSplineFollow();
                    break;
                case MoveState.Jumping:
                    UpdateSplineJump();
                    break;
            }
        }

        private void OnDisable()
        {
            CancelFollow();
        }


        #region 경로 이동 시작

        public bool BeginPath(
            SplinePath path,
            bool reverse = false,
            bool useGravity = false)
        {
            return BeginPathInternal(
                path,
                reverse ? 1f : 0f,
                reverse,
                useGravity);
        }

        public bool BeginClosestPath(bool useGravity = false)
        {
            if (_rigidBody == null ||
                !TryGetClosestPath(out SplinePath closestPath, out _, true))
            {
                return false;
            }

            float startT = closestPath.GetClosestEndT(GetFeetPosition());
            return BeginPathInternal(
                closestPath,
                startT,
                startT >= 1f,
                useGravity);
        }

        private bool BeginPathInternal(
            SplinePath path,
            float startT,
            bool reverse,
            bool useGravity)
        {
            if (!CanBeginPath(path))
                return false;

            if (IsFollowing)
                CancelFollow();

            SaveMovementState();

            currentPath = path;
            _currentT = Mathf.Clamp01(startT);
            _direction = reverse ? -1 : 1;
            _useGravity = useGravity;
            _speedMultiplier = 1f;
            _isCompleted = false;
            _state = MoveState.FollowingSpline;

            UpdateBodyToFeetOffset();
            ApplySplineMovementMode();
            SnapBodyToFeetPosition(
                currentPath.EvaluatePosition(_currentT),
                _useGravity);

            currentPath.MarkerManager?.ResetMarkers();
            ProcessMarkerAtCurrentPosition();
            return true;
        }

        private bool CanBeginPath(SplinePath path)
        {
            return path != null &&
                   path.isActiveAndEnabled &&
                   path.Length > Mathf.Epsilon &&
                   _baseMover != null &&
                   _rigidBody != null;
        }

        #endregion

        #region 스플라인 주행

        private void UpdateSplineFollow()
        {
            float pathLength = currentPath.Length;
            if (pathLength <= 0.001f)
            {
                Debug.Log("패쓰 근접으로 인한 컴플리트");
                CompleteFollow();
                return;
            }

            float previousT = _currentT;
            float deltaT = moveSpeed * _speedMultiplier * Time.fixedDeltaTime / pathLength;
            _currentT = Mathf.Clamp01(_currentT + deltaT * _direction);

            MoveBodyAlongSpline();
            ProcessMarkers(previousT);

            if (_state == MoveState.FollowingSpline && HasReachedPathEnd())
            {
                CompleteFollow();
            }
        }

        private void MoveBodyAlongSpline()
        {
            Vector2 bodyPosition = ToBodyPosition(
                currentPath.EvaluatePosition(_currentT));

            if (_useGravity)
            {
                _rigidBody.linearVelocityX =
                    (bodyPosition.x - _rigidBody.position.x) / Time.fixedDeltaTime;
                return;
            }

            _rigidBody.linearVelocity = Vector2.zero;
            _rigidBody.MovePosition(bodyPosition);
        }

        public void SetMoveSpeedMultiplier(float speedMultiplier)
            => _speedMultiplier = Mathf.Max(0f, speedMultiplier);

        private bool HasReachedPathEnd()
            => _direction > 0 ? _currentT >= 1f : _currentT <= 0f;

        #endregion

        #region 스플라인 점프

        public bool BeginSplineJump(
            int markerKnotIndex,
            SplineJumpSettings settings)
        {
            if (_state != MoveState.FollowingSpline ||
                currentPath == null || _baseMover == null || _rigidBody == null)
            {
                return false;
            }

            int nextKnotIndex = markerKnotIndex + _direction;
            if (!IsValidKnotIndex(markerKnotIndex) || !IsValidKnotIndex(nextKnotIndex))
                return false;

            _currentT = currentPath.GetKnotT(markerKnotIndex);
            _jumpTargetT = currentPath.GetKnotT(nextKnotIndex);
            _jumpStartFeetPosition = currentPath.EvaluatePosition(_currentT);
            _jumpSettings = settings;
            _jumpElapsedTime = 0f;
            _state = MoveState.Jumping;

            ApplyScriptedJumpMovementMode();
            SnapBodyToFeetPosition(_jumpStartFeetPosition);
            return true;
        }

        private void UpdateSplineJump()
        {
            _jumpElapsedTime += Time.fixedDeltaTime;
            float progress = Mathf.Clamp01(
                _jumpElapsedTime / _jumpSettings.Duration);

            Vector2 feetPosition = Vector2.Lerp(
                _jumpStartFeetPosition,
                GetJumpTargetFeetPosition(),
                progress);
            feetPosition.y +=
                4f * _jumpSettings.JumpHeight * progress * (1f - progress);

            _rigidBody.linearVelocity = Vector2.zero;
            _rigidBody.MovePosition(ToBodyPosition(feetPosition));

            if (progress >= 1f)
                CompleteSplineJump();
        }

        private void CompleteSplineJump()
        {
            float previousT = _currentT;
            AnimParamSO endAnimation = _jumpSettings.EndAnimation;

            _currentT = _jumpTargetT;
            _state = MoveState.FollowingSpline;

            ApplySplineMovementMode();
            SnapBodyToFeetPosition(GetJumpTargetFeetPosition());
            ClearJumpState();
            if (endAnimation != null)
                _boss.Renderer.PlayClip(endAnimation.ParamHash);
            ProcessMarkers(previousT);

            if (_state == MoveState.FollowingSpline && HasReachedPathEnd())
                CompleteFollow();
        }

        private void ClearJumpState()
        {
            _jumpSettings = default;
            _jumpElapsedTime = 0f;
            _jumpStartFeetPosition = Vector2.zero;
        }

        private Vector2 GetJumpTargetFeetPosition()
        {
            return currentPath.EvaluatePosition(_jumpTargetT);
        }

        #endregion

        #region 이동 종료 및 복구

        public void CancelFollow()
        {
            if (!IsFollowing)
                return;

            AnimParamSO endAnimation = _state == MoveState.Jumping
                ? _jumpSettings.EndAnimation
                : null;
            EndFollow(false, endAnimation);
        }

        private void CompleteFollow()
        {
            LastCompletedPath = currentPath;
            EndFollow(true);
        }

        private void EndFollow(bool completed, AnimParamSO endAnimation = null)
        {
            _state = MoveState.Idle;
            _isCompleted = completed;

            ClearJumpState();
            RestoreMovementState();
            if (endAnimation != null)
                _boss.Renderer.PlayClip(endAnimation.ParamHash);
        }

        private void SaveMovementState()
        {
            _previousCanManualMovement = _baseMover.CanManualMovement;
            _previousGravityScale = _rigidBody.gravityScale;
        }

        private void ApplySplineMovementMode()
        {
            _baseMover.CanManualMovement = false;
            _baseMover.StopImmediately(true, true);
            _rigidBody.gravityScale = _useGravity
                ? _previousGravityScale
                : 0f;
        }

        private void ApplyScriptedJumpMovementMode()
        {
            _baseMover.CanManualMovement = false;
            _baseMover.StopImmediately(true, true);
            _rigidBody.gravityScale = 0f;
        }

        private void RestoreMovementState()
        {
            if (_rigidBody != null)
            {
                _rigidBody.linearVelocity = Vector2.zero;
                _rigidBody.gravityScale = _previousGravityScale;
            }

            if (_baseMover != null)
            {
                _baseMover.SetMovementX(0f);
                _baseMover.CanManualMovement = _previousCanManualMovement;
            }
        }

        #endregion

        #region 경로 탐색

        public bool TryGetClosestPath(
            out SplinePath closestPath,
            out float closestT,
            bool excludeLastCompleted = false)
        {
            closestPath = null;
            closestT = 0f;

            if (_boss == null || _rigidBody == null || SplinePaths == null)
                return false;

            float closestSqrDistance = float.PositiveInfinity;
            Vector2 feetPosition = GetFeetPosition();

            foreach (SplinePath path in SplinePaths.Paths)
            {
                if (path == null || !path.isActiveAndEnabled)
                    continue;

                if (excludeLastCompleted && path == LastCompletedPath)
                    continue;

                float t = path.GetNearestPoint(feetPosition, out Vector3 nearestPosition);
                float sqrDistance = ((Vector3)feetPosition - nearestPosition).sqrMagnitude;
                if (sqrDistance >= closestSqrDistance)
                    continue;

                closestSqrDistance = sqrDistance;
                closestPath = path;
                closestT = t;
            }

            return closestPath != null;
        }

        #endregion

        private void ProcessMarkers(float previousT)
        {
            currentPath.MarkerManager?.Process(
                currentPath,
                previousT,
                _currentT,
                CreateEventContext());
        }

        private void ProcessMarkerAtCurrentPosition()
        {
            currentPath.MarkerManager?.ProcessAt(
                currentPath,
                _currentT,
                CreateEventContext());
        }

        private SplineEventContext CreateEventContext()
        {
            return new SplineEventContext(_boss, null, this);
        }

        private bool IsValidKnotIndex(int knotIndex)
        {
            return knotIndex >= 0 && knotIndex < currentPath.Spline.Count;
        }

        private void UpdateBodyToFeetOffset()
        {
            _bodyToFeetOffsetY =
                SplineAgentPositionUtility.GetBodyToFeetOffsetY(_rigidBody);
        }

        private Vector2 GetFeetPosition()
        {
            return SplineAgentPositionUtility.GetFeetPosition(
                _rigidBody,
                _bodyToFeetOffsetY);
        }

        private Vector2 ToBodyPosition(Vector2 feetPosition)
        {
            return SplineAgentPositionUtility.ToBodyPosition(
                feetPosition,
                _bodyToFeetOffsetY);
        }

        private void SnapBodyToFeetPosition(
            Vector2 feetPosition,
            bool preserveCurrentY = false)
        {
            Vector2 bodyPosition = ToBodyPosition(feetPosition);
            _rigidBody.position = preserveCurrentY
                ? new Vector2(bodyPosition.x, _rigidBody.position.y)
                : bodyPosition;
        }

    }
}
