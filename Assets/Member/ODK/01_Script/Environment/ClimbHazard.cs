using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Environment
{
    public class ClimbHazard : MonoBehaviour
    {
        public enum MotionType
        {
            Descend,
            Rotate,
            Sweep,
            Drop
        }

        private TowerClimbSequence owner;
        private MotionType motion;
        private float damage;
        private LayerMask playerLayer;
        private float rotateSpeed;
        private float sweepAmplitude;
        private float sweepFrequency;
        private float sweepPhase;
        private float dropSpeed;
        private float dropDelay;
        private float anchorX;
        private float elapsed;
        private float hitCooldown = 0.5f;
        private float nextHitTime;
        private LineRenderer warningLine;

        public void Initialize(
            TowerClimbSequence sequence,
            MotionType motionType,
            float hazardDamage,
            LayerMask targetLayer)
        {
            owner = sequence;
            motion = motionType;
            damage = hazardDamage;
            playerLayer = targetLayer;
            anchorX = transform.position.x;
            elapsed = 0f;
        }

        public void ConfigureRotate(float degreesPerSecond) => rotateSpeed = degreesPerSecond;

        public void ConfigureSweep(float amplitude, float frequency)
        {
            sweepAmplitude = amplitude;
            sweepFrequency = frequency;
            sweepPhase = Random.Range(0f, Mathf.PI * 2f);
        }

        public void ConfigureDrop(float speed, float delay, LineRenderer warning)
        {
            dropSpeed = speed;
            dropDelay = delay;
            warningLine = warning;
        }

        private void Update()
        {
            if (owner == null)
            {
                Destroy(gameObject);
                return;
            }

            float delta = Time.deltaTime;
            elapsed += delta;
            Vector3 position = transform.position;

            if (motion == MotionType.Drop)
            {
                if (elapsed < dropDelay)
                {
                    UpdateWarning(elapsed / Mathf.Max(0.01f, dropDelay));
                    return;
                }
                if (warningLine != null) warningLine.enabled = false;
                position.y -= (dropSpeed + owner.WorldFallSpeed) * delta;
            }
            else
            {
                position.y -= owner.WorldFallSpeed * delta;
                if (motion == MotionType.Rotate)
                    transform.Rotate(0f, 0f, rotateSpeed * delta);
                else if (motion == MotionType.Sweep)
                    position.x = anchorX + Mathf.Sin(elapsed * sweepFrequency * Mathf.PI * 2f + sweepPhase) * sweepAmplitude;
            }

            transform.position = position;
            if (position.y < owner.DespawnY) Destroy(gameObject);
        }

        private void UpdateWarning(float rate)
        {
            if (warningLine == null) return;
            Color color = new Color(1f, 1f, 1f, Mathf.Lerp(0.15f, 0.45f, Mathf.PingPong(rate * 6f, 1f)));
            warningLine.startColor = color;
            warningLine.endColor = new Color(1f, 1f, 1f, color.a * 0.3f);
            Vector3 top = transform.position;
            warningLine.SetPosition(0, top);
            warningLine.SetPosition(1, new Vector3(top.x, owner.DespawnY, top.z));
        }

        private void OnTriggerEnter2D(Collider2D other) => TryHit(other);
        private void OnTriggerStay2D(Collider2D other) => TryHit(other);

        private void TryHit(Collider2D other)
        {
            if (other == null || Time.time < nextHitTime) return;
            if (motion == MotionType.Drop && elapsed < dropDelay) return;
            int mask = 1 << other.gameObject.layer;
            bool isPlayer = (playerLayer.value & mask) != 0 || other.transform.root.CompareTag("Player");
            if (!isPlayer) return;
            if (!DamageCaster.IsWithinPlayerHitbox(GetComponent<Collider2D>(), other)) return;
            nextHitTime = Time.time + hitCooldown;
            DamageCaster.ApplyDamage(other.transform, new DamageData(damage, DamageType.Projectile));
            owner?.NotifyHazardHit(transform.position);
        }
    }
}
