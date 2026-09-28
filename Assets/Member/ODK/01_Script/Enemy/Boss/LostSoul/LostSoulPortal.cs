using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    public class LostSoulPortal
    {
        private readonly LostSoul owner;
        private float nextPulseTime;

        public Vector3 Position { get; }
        public bool IsClosed { get; private set; }
        public Coroutine Loop { get; set; }

        public LostSoulPortal(LostSoul owner, Vector3 position)
        {
            this.owner = owner;
            Position = position;
        }

        public void Pulse()
        {
            if (IsClosed || owner == null || Time.time < nextPulseTime) return;
            nextPulseTime = Time.time + 0.15f;
            owner.PlayPortalPulse(Position);
        }

        public void Close(float duration = 0f)
        {
            if (IsClosed) return;
            IsClosed = true;
            if (owner != null) owner.StopPortal(this);
        }
    }
}
