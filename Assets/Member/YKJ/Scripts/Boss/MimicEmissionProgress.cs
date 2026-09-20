using System;

namespace Member.YKJ.Bosses
{
    // Contains no Unity timing: pausing means not advancing this counter.
    public sealed class MimicEmissionProgress
    {
        private readonly int _total;
        private readonly float _interval;
        private float _elapsed;

        public int Count { get; private set; }
        public bool Finished => Count >= _total;

        public MimicEmissionProgress(int total, float interval)
        {
            if (total < 1) throw new ArgumentOutOfRangeException(nameof(total));
            if (interval <= 0f || float.IsNaN(interval) || float.IsInfinity(interval))
                throw new ArgumentOutOfRangeException(nameof(interval));
            _total = total;
            _interval = interval;
        }

        public bool Advance(float deltaTime)
        {
            if (Finished || deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                return false;
            _elapsed += deltaTime;
            if (_elapsed < _interval)
                return false;

            // Do not burst multiple weapons after a long frame or an interruption.
            _elapsed = 0f;
            Count++;
            return true;
        }

        public bool IsTongueCheckpoint => !Finished &&
            (Count == (_total + 2) / 3 || Count == (_total * 2 + 2) / 3);
    }
}
