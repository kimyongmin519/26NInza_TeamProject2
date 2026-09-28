using System;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    [Serializable]
    public class MoonDashMotion
    {
        [SerializeField] private float chaseAcceleration = 30f;
        [SerializeField] private float dashImpulse = 22f;
        [SerializeField] private float cruiseMaxSpeed = 14f;
        [SerializeField] private float overspeedDamping = 16f;
        [SerializeField] private float drag = 0.6f;
        [SerializeField] private float dashInterval = 0.58f;
        [SerializeField] private float firstDashDelay = 0.1f;

        public Vector2 Velocity { get; private set; }
        public float Speed => Velocity.magnitude;
        public float DashInterval => dashInterval * intervalScale;
        public float DashImpulse => dashImpulse * powerScale;

        private float dashTimer;
        private float speedScale = 1f;
        private float powerScale = 1f;
        private float intervalScale = 1f;

        public MoonDashMotion() { }

        public MoonDashMotion(MoonDashMotion source)
        {
            if (source == null) return;
            chaseAcceleration = source.chaseAcceleration;
            dashImpulse = source.dashImpulse;
            cruiseMaxSpeed = source.cruiseMaxSpeed;
            overspeedDamping = source.overspeedDamping;
            drag = source.drag;
            dashInterval = source.dashInterval;
            firstDashDelay = source.firstDashDelay;
        }

        public void Begin(Vector2 initialVelocity, float scale = 1f, float power = 1f, float interval = 1f)
        {
            speedScale = Mathf.Max(0.01f, scale);
            powerScale = Mathf.Max(0.05f, power);
            intervalScale = Mathf.Max(0.05f, interval);
            Velocity = initialVelocity;
            dashTimer = firstDashDelay / speedScale;
        }

        public bool Tick(Vector2 position, Vector2 targetPosition, float deltaTime)
        {
            Vector2 toTarget = targetPosition - position;
            Vector2 direction = toTarget.sqrMagnitude > 0.0001f
                ? toTarget.normalized
                : (Velocity.sqrMagnitude > 0.0001f ? Velocity.normalized : Vector2.right);

            Vector2 velocity = Velocity;
            velocity += direction * chaseAcceleration * powerScale * speedScale * speedScale * deltaTime;
            velocity *= 1f / (1f + drag * deltaTime);

            float cruise = cruiseMaxSpeed * powerScale * speedScale;
            float speed = velocity.magnitude;
            if (speed > cruise)
                velocity = velocity.normalized * Mathf.MoveTowards(speed, cruise, overspeedDamping * speedScale * deltaTime);

            bool dashed = false;
            dashTimer -= deltaTime;
            if (dashTimer <= 0f)
            {
                velocity = direction * dashImpulse * powerScale * speedScale;
                dashTimer += Mathf.Max(0.05f, DashInterval / speedScale);
                dashed = true;
            }

            Velocity = velocity;
            return dashed;
        }

        public void Bounce(Vector2 normal, float keep = 0.6f)
        {
            if (normal.sqrMagnitude < 0.0001f) return;
            Velocity = Vector2.Reflect(Velocity, normal.normalized) * keep;
        }

        public void Stop()
        {
            Velocity = Vector2.zero;
        }
    }
}
