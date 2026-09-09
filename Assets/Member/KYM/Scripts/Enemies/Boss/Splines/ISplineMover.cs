using UnityEngine;

namespace Member.KYM.Scripts.Enemies.Boss.Splines
{
    public interface ISplineMover
    {
        bool IsGrounded { get; }
        bool CanManualMovement { get; set; }
        public SplinePath[] SplinePaths { get; }
        public void SetMoveSpeedMultiplier(float speedMultiplier);
        public bool TryGetClosestPath(out SplinePath closestPath, out float closestT);
    }
}