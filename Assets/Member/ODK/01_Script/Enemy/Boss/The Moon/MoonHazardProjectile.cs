using DG.Tweening;
using GGMLib.ObjectPool.Runtime;
using KimLIb.SoundSystem;
using Member.ODK.Scripts.Enemys.Bosses;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class MoonHazardProjectile : AbstractMonoPoolable, ICancellableBossSpawn
    {
        [SerializeField] private Vector2 fallingAngularSpeedRange = new Vector2(-320f, 320f);
        [SerializeField] private SoundClipSO impactSound;

        public enum MoveMode
        {
            Linear,
            Falling,
            Homing,
            Dash
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
        private MoonDashMotion dashMotion;
        private Vector3 dashBaseScale = Vector3.one;
        private SpriteRenderer spriteRenderer;
        private Sprite defaultSprite;
        private CircleCollider2D circle;
        private float defaultRadius = 0.35f;
        private Vector3 defaultScale = Vector3.one;
        private Color defaultColor = Color.white;
        private bool defaultColliderEnabled = true;

        public override void ResetItem()
        {
            transform.DOKill();
            consumed = false;
            dashMotion = null;
            target = null;
            if (body == null) CacheComponents();
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.gravityScale = 0f;
            body.simulated = true;
            transform.localScale = defaultScale;
            transform.rotation = Quaternion.identity;
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = defaultSprite;
                spriteRenderer.color = defaultColor;
            }
            if (circle != null)
            {
                circle.radius = defaultRadius;
                circle.enabled = defaultColliderEnabled;
            }
        }

        private void CacheComponents()
        {
            body = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && defaultSprite == null)
            {
                defaultSprite = spriteRenderer.sprite;
                defaultColor = spriteRenderer.color;
                defaultScale = transform.localScale;
            }
            circle = GetComponent<CircleCollider2D>();
            if (circle != null)
            {
                defaultRadius = circle.radius;
                defaultColliderEnabled = circle.enabled;
            }
        }

        public void SetDashBaseScale(Vector3 scale)
        {
            dashBaseScale = scale;
            transform.localScale = scale;
        }

        public void Initialize(
            Vector2 velocity,
            MoveMode moveMode,
            Transform homingTarget,
            float projectileDamage,
            float lifeTime,
            LayerMask playerMask,
            LayerMask groundMask,
            Sprite sprite = null,
            Color? color = null,
            float fallingGravity = 1.2f)
        {
            if (body == null) CacheComponents();
            consumed = false;
            dashMotion = null;
            name = $"Moon {moveMode} Projectile";
            target = homingTarget;
            mode = moveMode;
            playerLayer = playerMask;
            groundLayer = groundMask;
            damage = projectileDamage;
            speed = Mathf.Max(0.1f, velocity.magnitude);
            turnSpeed = 3.5f;
            lifeRemaining = Mathf.Max(0.2f, lifeTime);
            body.gravityScale = moveMode == MoveMode.Falling ? Mathf.Max(0.05f, fallingGravity) : 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.linearVelocity = velocity;
            body.angularVelocity = moveMode == MoveMode.Falling
                ? Random.Range(
                    Mathf.Min(fallingAngularSpeedRange.x, fallingAngularSpeedRange.y),
                    Mathf.Max(fallingAngularSpeedRange.x, fallingAngularSpeedRange.y)
                )
                : 0f;
            if (circle != null) circle.radius = moveMode == MoveMode.Homing || moveMode == MoveMode.Dash ? 0.65f : defaultRadius;
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite != null ? sprite : defaultSprite;
                spriteRenderer.color = color ?? new Color(0.75f, 0.85f, 1f, 0.9f);
            }
        }

        public void UseDashMotion(MoonDashMotion settings, float speedScale = 1f, float power = 1f, float interval = 1f)
        {
            mode = MoveMode.Dash;
            body.gravityScale = 0f;
            body.angularVelocity = 0f;
            dashMotion = new MoonDashMotion(settings);
            dashBaseScale = transform.localScale;
            dashMotion.Begin(body.linearVelocity, speedScale, power, interval);
            if (circle != null) circle.radius = 0.65f;
        }

        private void Awake()
        {
            if (body == null) CacheComponents();
        }

        private void Update()
        {
            if (consumed) return;
            lifeRemaining -= Time.deltaTime;
            if (lifeRemaining <= 0f) Consume();
        }

        private void FixedUpdate()
        {
            if (consumed) return;
            if (mode == MoveMode.Dash && dashMotion != null)
            {
                Vector2 targetPosition = target != null ? (Vector2)target.position : body.position + body.linearVelocity;
                dashMotion.Tick(body.position, targetPosition, Time.fixedDeltaTime);
                body.linearVelocity = dashMotion.Velocity;
                if (body.linearVelocity.sqrMagnitude > 0.01f)
                {
                    float angle = Mathf.Atan2(body.linearVelocity.y, body.linearVelocity.x) * Mathf.Rad2Deg - 90f;
                    body.MoveRotation(angle);
                }
                return;
            }
            if (mode != MoveMode.Homing || target == null) return;
            Vector2 desired = ((Vector2)target.position - body.position).normalized;
            Vector2 current = body.linearVelocity.sqrMagnitude > 0.01f
                ? body.linearVelocity.normalized
                : desired;
            float rate = 1f - Mathf.Exp(-turnSpeed * Time.fixedDeltaTime);
            body.linearVelocity = Vector2.Lerp(current, desired, rate).normalized * speed;
        }

        private void OnTriggerStay2D(Collider2D other) => OnTriggerEnter2D(other);

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed || other == null) return;
            int mask = 1 << other.gameObject.layer;
            bool player = (playerLayer.value & mask) != 0 ||
                other.CompareTag("Player") || other.transform.root.CompareTag("Player");
            if (player)
            {
                if (!DamageCaster.IsWithinPlayerHitbox(circle != null ? circle : GetComponent<Collider2D>(), other)) return;
                DamageCaster.ApplyDamage(
                    other.transform,
                    new DamageData(damage, DamageType.Projectile)
                );
                ODKSoundPlayback.Play(impactSound, transform.position);
                Consume();
                return;
            }

            if (mode == MoveMode.Falling && (groundLayer.value & mask) != 0)
            {
                ODKSoundPlayback.Play(impactSound, transform.position);
                Consume();
            }
        }

        private void Consume()
        {
            if (consumed) return;
            consumed = true;
            transform.DOKill();
            ODKPool.Despawn(this);
        }

        public void CancelBossSpawn() => Consume();
    }
}
