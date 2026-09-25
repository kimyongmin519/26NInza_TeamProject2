using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class MoonHazardProjectile : MonoBehaviour
    {
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

        public static MoonHazardProjectile Create(
            Vector3 position,
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
            GameObject projectileObject = new GameObject($"Moon {moveMode} Projectile");
            projectileObject.transform.position = position;
            Rigidbody2D rigidbody = projectileObject.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = moveMode == MoveMode.Falling ? 2.2f : 0f;
            rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rigidbody.linearVelocity = velocity;
            CircleCollider2D collider = projectileObject.AddComponent<CircleCollider2D>();
            collider.radius = moveMode == MoveMode.Homing ? 0.65f : 0.35f;
            collider.isTrigger = true;

            SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite != null
                ? sprite
                : Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            renderer.color = color ?? new Color(0.75f, 0.85f, 1f, 0.9f);
            renderer.sortingOrder = 24;

            MoonHazardProjectile projectile = projectileObject.AddComponent<MoonHazardProjectile>();
            projectile.body = rigidbody;
            projectile.target = homingTarget;
            projectile.mode = moveMode;
            projectile.playerLayer = playerMask;
            projectile.groundLayer = groundMask;
            projectile.damage = projectileDamage;
            projectile.speed = Mathf.Max(0.1f, velocity.magnitude);
            projectile.turnSpeed = 3.5f;
            projectile.lifeRemaining = Mathf.Max(0.2f, lifeTime);
            return projectile;
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
                Consume();
                return;
            }

            if (mode != MoveMode.Linear && (groundLayer.value & mask) != 0)
                Consume();
        }

        private void Consume()
        {
            if (consumed) return;
            consumed = true;
            Destroy(gameObject);
        }
    }
}
