using System.Collections.Generic;
using Member.KYM.Scripts.Players.RobotArm;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class MoonRock : GrabbableRigidbody
    {
        [SerializeField] private float armDelay = 0.18f;

        private MoonBoss owner;
        private float bossDamage;
        private float playerDamage;
        private float explosionRadius;
        private float lifeRemaining;
        private float armedTime;
        private LayerMask playerLayer;
        private LayerMask groundLayer;
        private bool isThrown;
        private bool exploded;
        private Vector2 previousPosition;

        public static MoonRock CreateFallback(Vector3 position)
        {
            GameObject rockObject = new GameObject("Moon Rock");
            rockObject.transform.position = position;
            Rigidbody2D body = rockObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 2.4f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            CircleCollider2D collider = rockObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;

            SpriteRenderer renderer = rockObject.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f
            );
            renderer.color = new Color(0.55f, 0.58f, 0.65f, 1f);
            renderer.sortingOrder = 25;
            return rockObject.AddComponent<MoonRock>();
        }

        public void Initialize(
            MoonBoss moonBoss,
            Vector2 velocity,
            float damageToBoss,
            float damageToPlayer,
            float blastRadius,
            float lifeTime,
            LayerMask playerMask,
            LayerMask groundMask)
        {
            owner = moonBoss;
            bossDamage = Mathf.Max(0f, damageToBoss);
            playerDamage = Mathf.Max(0f, damageToPlayer);
            explosionRadius = Mathf.Max(0.1f, blastRadius);
            lifeRemaining = Mathf.Max(0.5f, lifeTime);
            playerLayer = playerMask;
            groundLayer = groundMask;
            armedTime = Time.time + Mathf.Max(0f, armDelay);
            Rigidbody.linearVelocity = velocity;
            Rigidbody.angularVelocity = Random.Range(-300f, 300f);
            previousPosition = Rigidbody.position;
        }

        private void Update()
        {
            if (exploded || IsHeld) return;
            lifeRemaining -= Time.deltaTime;
            if (lifeRemaining <= 0f) Explode(false);
        }

        private void FixedUpdate()
        {
            if (!isThrown || exploded || IsHeld)
            {
                previousPosition = Rigidbody.position;
                return;
            }

            Vector2 currentPosition = Rigidbody.position;
            foreach (RaycastHit2D hit in Physics2D.LinecastAll(previousPosition, currentPosition))
            {
                if (hit.collider == null || hit.rigidbody == Rigidbody) continue;
                if (TryHitBoss(hit.collider)) break;
            }
            previousPosition = currentPosition;
        }

        protected override void OnGrabbed()
        {
            isThrown = false;
            previousPosition = Rigidbody.position;
        }

        protected override void OnReleased()
        {
            isThrown = false;
            previousPosition = Rigidbody.position;
        }

        protected override void OnThrown(ThrowData throwData)
        {
            isThrown = true;
            armedTime = Time.time;
            Rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            previousPosition = Rigidbody.position;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleContact(collision.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleContact(other);
        }

        private void HandleContact(Collider2D other)
        {
            if (exploded || IsHeld || other == null) return;
            if (TryHitBoss(other)) return;
            if (Time.time < armedTime) return;

            int layerMask = 1 << other.gameObject.layer;
            bool hitGround = (groundLayer.value & layerMask) != 0;
            bool hitPlayer = (playerLayer.value & layerMask) != 0 ||
                other.CompareTag("Player") || other.transform.root.CompareTag("Player");
            if (hitGround || hitPlayer) Explode(true);
        }

        private bool TryHitBoss(Collider2D other)
        {
            if (!isThrown || owner == null) return false;
            MoonBoss hitBoss = other.GetComponentInParent<MoonBoss>();
            if (hitBoss == null || hitBoss != owner) return false;

            hitBoss.TakeDamage(new DamageData(bossDamage, DamageType.Projectile));
            Explode(false);
            return true;
        }

        private void Explode(bool damagePlayer)
        {
            if (exploded) return;
            exploded = true;

            if (damagePlayer)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(
                    transform.position,
                    explosionRadius,
                    playerLayer
                );
                HashSet<Transform> damagedRoots = new HashSet<Transform>();
                foreach (Collider2D hit in hits)
                {
                    Transform root = hit.transform.root;
                    if (!damagedRoots.Add(root)) continue;
                    DamageCaster.ApplyDamage(
                        root,
                        new DamageData(playerDamage, DamageType.Special)
                    );
                }
            }

            owner?.ShakeImpact(false);
            owner?.AttackImpact(transform.position);
            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.45f, 0.15f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
