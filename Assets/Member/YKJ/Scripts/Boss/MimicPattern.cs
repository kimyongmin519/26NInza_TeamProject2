using System;

namespace Member.YKJ.Bosses
{
    [Serializable]
    public abstract class MimicPattern
    {
        protected MimicBoss Boss { get; private set; }

        public virtual void Initialize(MimicBoss boss) => Boss = boss;
        public virtual bool CanStart() => true;
        public virtual void OnStart() { }
        public virtual void OnUpdate(float deltaTime) { }
        public virtual void OnPause() { }
        public virtual void OnResume() { }
        public virtual void OnEnd() { }
        public virtual void OnDie() { }

        protected void EndPattern() => Boss.Patterns.Complete(this);
        protected bool InterruptWith(MimicPattern pattern) => Boss.Patterns.Interrupt(this, pattern);
    }
}
