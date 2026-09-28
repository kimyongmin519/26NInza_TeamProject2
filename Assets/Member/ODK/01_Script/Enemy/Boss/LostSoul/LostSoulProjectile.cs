using GGMLib.ObjectPool.Runtime;
using KimLIb.SoundSystem;
using Member.ODK.Scripts.Enemys.Bosses;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class LostSoulProjectile : AbstractMonoPoolable, ICancellableBossSpawn
    {
        [SerializeField] private float lifeTime = 6f;
        [SerializeField] private float acceleration = 9f;
        [SerializeField] private float homingDegreesPerSecond = 6f;
        [SerializeField] private float visualAngleOffset = 180f;
        [SerializeField] private SoundClipSO impactSound;
        private LostSoul owner;
        private float damage;
        private LayerMask playerLayer;
        private Rigidbody2D body;
        private Transform target;
        private float speed;
        private float maxSpeed;
        private Vector2 launchDirection;
        private bool consumed;
        private float lifeRemaining;

        public override void ResetItem()
        {
            consumed = false;
            owner = null;
            target = null;
            speed = 0f;
            maxSpeed = 0f;
            if (body == null) body = GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.gravityScale = 0f;
            body.simulated = true;
        }

        public void Initialize(
            LostSoul source,
            Vector2 velocity,
            float amount,
            LayerMask targetLayer,
            Transform homingTarget)
        {
            owner = source;
            damage = amount;
            playerLayer = targetLayer;
            target = homingTarget;
            body = GetComponent<Rigidbody2D>();
            maxSpeed = velocity.magnitude;
            speed = 0f;
            launchDirection = velocity.sqrMagnitude > 0.01f ? velocity.normalized : Vector2.right;
            body.linearVelocity = Vector2.zero;
            SetVisualDirection(launchDirection);
            consumed = false;
            lifeRemaining = lifeTime;
        }

        private void Update()
        {
            if (consumed) return;
            lifeRemaining -= Time.deltaTime;
            if (lifeRemaining <= 0f) Consume();
        }

        private void Consume()
        {
            if (consumed) return;
            consumed = true;
            if (body != null) body.linearVelocity = Vector2.zero;
            ODKPool.Despawn(this);
        }

        public void CancelBossSpawn() => Consume();

        private void FixedUpdate()
        {
            if (body == null || consumed || maxSpeed <= 0.01f) return;
            speed = Mathf.MoveTowards(speed, maxSpeed, acceleration * Time.fixedDeltaTime);
            Vector2 current = body.linearVelocity.sqrMagnitude > 0.01f
                ? body.linearVelocity.normalized
                : launchDirection;
            Vector2 direction = current;
            if (target != null)
            {
                Vector2 desired = ((Vector2)target.position - body.position).normalized;
                float radians = homingDegreesPerSecond * Mathf.Deg2Rad * Time.fixedDeltaTime;
                direction = Vector3.RotateTowards(current, desired, radians, 0f);
            }
            body.linearVelocity = direction.normalized * speed;
            launchDirection = direction.normalized;
            SetVisualDirection(launchDirection);
        }

        private void SetVisualDirection(Vector2 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + visualAngleOffset;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void OnTriggerStay2D(Collider2D other) => OnTriggerEnter2D(other);

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed || other == null) return;
            int mask = 1 << other.gameObject.layer;
            if ((playerLayer.value & mask) == 0 && !other.transform.root.CompareTag("Player")) return;
            if (!DamageCaster.IsWithinPlayerHitbox(GetComponent<Collider2D>(), other)) return;
            DamageCaster.ApplyDamage(other.transform, new DamageData(damage, DamageType.Projectile));
            ODKSoundPlayback.Play(impactSound, transform.position);
            owner?.PlaySoulImpactFeedback(transform.position);
            Consume();
        }
    }
}
