namespace Member.KYM.Scripts.Enemies.Boss.Splines
{
    public interface ISplineMover
    {
        bool IsFollowing { get; }
        bool IsCompleted { get; }
        SplinePath LastCompletedPath { get; }
        SplinePaths SplinePaths { get; }

        bool BeginPath(
            SplinePath path,
            bool reverse = false,
            bool useGravity = false);
        bool BeginClosestPath(bool useGravity = false);
        void CancelFollow();
        void SetMoveSpeedMultiplier(float speedMultiplier);
        bool TryGetClosestPath(
            out SplinePath closestPath,
            out float closestT,
            bool excludeLastCompleted = false);
    }
}
