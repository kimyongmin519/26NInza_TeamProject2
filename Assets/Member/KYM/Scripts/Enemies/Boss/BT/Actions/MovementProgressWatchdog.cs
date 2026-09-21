namespace Member.KYM.Scripts.Enemies.Boss.BT.Actions
{
    internal enum MovementProgressState
    {
        Moving,
        Stalled,
        TimedOut
    }

    internal sealed class MovementProgressWatchdog
    {
        private const float MinimumProgress = 0.02f;

        private float _elapsedTime;
        private float _noProgressTime;
        private float _bestDistance;

        public void Reset(float initialDistance)
        {
            _elapsedTime = 0f;
            _noProgressTime = 0f;
            _bestDistance = initialDistance;
        }

        public MovementProgressState Update(
            float currentDistance,
            float deltaTime,
            bool measureProgress,
            float noProgressDuration,
            float maxDuration)
        {
            _elapsedTime += deltaTime;
            if (_elapsedTime >= maxDuration)
                return MovementProgressState.TimedOut;

            if (!measureProgress)
            {
                _noProgressTime = 0f;
                return MovementProgressState.Moving;
            }

            if (currentDistance <= _bestDistance - MinimumProgress)
            {
                _bestDistance = currentDistance;
                _noProgressTime = 0f;
                return MovementProgressState.Moving;
            }

            _noProgressTime += deltaTime;
            return _noProgressTime >= noProgressDuration
                ? MovementProgressState.Stalled
                : MovementProgressState.Moving;
        }

        public void ResetStall(float currentDistance)
        {
            _noProgressTime = 0f;
            _bestDistance = currentDistance;
        }
    }
}
