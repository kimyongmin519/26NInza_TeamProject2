using System;
using System.Collections.Generic;

namespace Member.YKJ.Bosses
{
    // Lifecycle callbacks only initialize/clean up; transitions happen during OnUpdate.
    public sealed class MimicPatternRunner
    {
        private readonly Stack<MimicPattern> _suspended = new Stack<MimicPattern>();
        private bool _transitioning;

        public MimicPattern Current { get; private set; }
        public bool IsRunning => Current != null;
        public int SuspendedCount => _suspended.Count;

        public bool Start(MimicPattern pattern)
        {
            if (_transitioning || IsRunning || pattern == null || !pattern.CanStart())
                return false;

            Transition(() =>
            {
                Current = pattern;
                pattern.OnStart();
            });
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (!_transitioning && deltaTime > 0f && !float.IsNaN(deltaTime) && !float.IsInfinity(deltaTime))
                Current?.OnUpdate(deltaTime);
        }

        public bool Interrupt(MimicPattern caller, MimicPattern next)
        {
            if (_transitioning || caller == null || caller != Current || next == null ||
                next == caller || _suspended.Contains(next) || !next.CanStart())
                return false;

            Transition(() =>
            {
                caller.OnPause();
                _suspended.Push(caller);
                Current = next;
                next.OnStart();
            });
            return true;
        }

        public bool Complete(MimicPattern caller)
        {
            if (_transitioning || caller == null || caller != Current)
                return false;

            Transition(() =>
            {
                Current = null;
                caller.OnEnd();
                if (_suspended.Count == 0)
                    return;

                Current = _suspended.Pop();
                Current.OnResume();
            });
            return true;
        }

        public void Cancel(bool died = false)
        {
            if (_transitioning)
                return;

            Transition(() =>
            {
                MimicPattern current = Current;
                Current = null;
                EndCancelled(current, died);
                while (_suspended.Count > 0)
                    EndCancelled(_suspended.Pop(), died);
            });
        }

        private static void EndCancelled(MimicPattern pattern, bool died)
        {
            if (pattern == null)
                return;
            if (died)
                pattern.OnDie();
            pattern.OnEnd();
        }

        private void Transition(Action action)
        {
            _transitioning = true;
            try { action(); }
            finally { _transitioning = false; }
        }
    }
}
