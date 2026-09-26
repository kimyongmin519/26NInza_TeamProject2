using KimLIb.SoundSystem;
using Member.ODK.Scripts.Enemys.Bosses;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class MoonHazardProjectile : MonoBehaviour
    {
        [SerializeField] private Vector2 fallingAngularSpeedRange = new Vector2(-320f, 320f);
        [SerializeField] private SoundClipSO impactSound;

        public enum MoveMode
        {
            Linear,
            Falling,
            Homing
        }

        private Rigidbody2D body;
        private Transform target;
        private MoveMode mode;
        private LayerMask playerLayer;
        private LayerMask groundLayer;
        private float damage;
        private float speed;
        private float turnSpeed;
        private float lifeRemaining;
        private bool consumed;

        public void Initialize(
            Vector2 velocity,
            MoveMode moveMode,
            Transform homingTarget,
            float projectileDamage,
            float lifeTime,
            LayerMask playerMask,
            LayerMask groundMask,
            Sprite sprite = null,
            Color? color = null)
        {
            name = $"Moon {moveMode} Projectile";
            target = homingTarget;
            mode = moveMode;
            playerLayer = playerMask;
            groundLayer = groundMask;
            damage = projectileDamage;
            speed = Mathf.Max(0.1f, velocity.magnitude);
            turnSpeed = 3.5f;
            lifeRemaining = Mathf.Max(0.2f, lifeTime);
            body.gravityScale = moveMode == MoveMode.Falling ? 2.2f : 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.linearVelocity = velocity;
            body.angularVelocity = moveMode == MoveMode.Falling
                ? Random.Range(
                    Mathf.Min(fallingAngularSpeedRange.x, fallingAngularSpeedRange.y),
                    Mathf.Max(fallingAngularSpeedRange.x, fallingAngularSpeedRange.y)
                )
                : 0f;
            CircleCollider2D circle = GetComponent<CircleCollider2D>();
            if (circle != null) circle.radius = moveMode == MoveMode.Homing ? 0.65f : 0.35f;
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                if (sprite != null) renderer.sprite = sprite;
                renderer.color = color ?? new Color(0.75f, 0.85f, 1f, 0.9f);
            }
        }

        private void Awake()
        {
            if (body == null) body = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            lifeRemaining -= Time.deltaTime;
            if (lifeRemaining <= 0f) Consume();
        }

        private void FixedUpdate()
        {
            if (consumed || mode != MoveMode.Homing || target == null) return;
            Vector2 desired = ((Vector2)target.position - body.position).normalized;
            Vector2 current = body.linearVelocity.sqrMagnitude > 0.01f
                ? body.linearVelocity.normalized
                : desired;
            float rate = 1f - Mathf.Exp(-turnSpeed * Time.fixedDeltaTime);
            body.linearVelocity = Vector2.Lerp(current, desired, rate).normalized * speed;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed || other == null) return;
            int mask = 1 << other.gameObject.layer;
            bool player = (playerLayer.value & mask) != 0 ||
                other.CompareTag("Player") || other.transform.root.CompareTag("Player");
            if (player)
            {
                DamageCaster.ApplyDamage(
                    other.transform,
                    new DamageData(damage, DamageType.Projectile)
                );
                ODKSoundPlayback.Play(impactSound, transform.position);
                Consume();
                return;
            }

            if (mode != MoveMode.Linear && (groundLayer.value & mask) != 0)
            {
                ODKSoundPlayback.Play(impactSound, transform.position);
                Consume();
            }
        }

        private void Consume()
        {
            if (consumed) return;
            consumed = true;
            Destroy(gameObject);
        }
    }
}
