using KimLIb.ModuleSystems;
using Member.KYM.Scripts.Agents;
using UnityEngine;

namespace Member.KYM.Scripts.Enemies.Boss.Splines
{
    public class BossSplineMover : MonoBehaviour, ISplineMover, IModule, IAfterInitModule
    {
        [SerializeField] private float moveSpeed = 5f;
        [field:SerializeField] public SplinePath[] SplinePaths { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool CanManualMovement { get; set; }
        
        
        [SerializeField] private SplinePath currentPath;
        private AbstractBoss _boss;
        private IMover _baseMover;
        private Rigidbody2D _rigidBody;

        private float _currentT;
        private float _moveSpeedMultiplier = 1f;
        private float _pauseRemaining;

        public bool IsFollowing { get; private set; }
        public bool IsCompleted { get; private set; }

        public void Initialize(ModuleOwner owner)
        {
            _boss = owner as AbstractBoss;
            Debug.Assert(_boss != null, "_boss is null");

            _baseMover = owner.GetModule<IMover>();
        }
        
        public void AfterInit()
        {
            _rigidBody = _baseMover.RigidBody;
        }
        
        public void SetMoveSpeedMultiplier(float speedMultiplier)
        {
            
        }

        public bool BeginClosestPath()
        {
            if (!TryGetClosestPath(out currentPath, out _currentT))
                return false;

            IsFollowing = true;
            IsCompleted = false;

            _moveSpeedMultiplier = 1f;
            _pauseRemaining = 0f;

            _baseMover.CanManualMovement = false;
            _baseMover.StopImmediately(true, true);
            _baseMover.SetGravityScale(0f);

            currentPath.MarkerManager.ResetMarkers(_currentT);

            return true;
        }

        public bool TryGetClosestPath(out SplinePath closestPath, out float closestT)
        {
            closestPath = null;
            closestT = 0f;

            float closestSqrDistance = float.PositiveInfinity;
            Vector3 bossPosition = _boss.transform.position;

            foreach (SplinePath path in SplinePaths)
            {
                if (path == null || !path.isActiveAndEnabled)
                    continue;

                float t = path.GetNearestPoint(
                    bossPosition,
                    out Vector3 nearestPosition);

                float sqrDistance =
                    (bossPosition - nearestPosition).sqrMagnitude;

                if (sqrDistance >= closestSqrDistance)
                    continue;

                closestSqrDistance = sqrDistance;
                closestPath = path;
                closestT = t;
            }

            return closestPath != null;
        }
    }
}