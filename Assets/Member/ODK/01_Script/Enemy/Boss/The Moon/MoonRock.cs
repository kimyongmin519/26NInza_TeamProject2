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
        private bool isThrown;
        private bool exploded;
        private Vector2 previousPosition;
        [SerializeField] private DamageCaster explosionCaster;

        public void Initialize(
            MoonBoss moonBoss,
            Vector2 velocity,
            float damageToBoss,
            float damageToPlayer,
            float blastRadius,
            float lifeTime,
            LayerMask playerMask)
        {
            owner = moonBoss;
            bossDamage = Mathf.Max(0f, damageToBoss);
            playerDamage = Mathf.Max(0f, damageToPlayer);
            explosionRadius = Mathf.Max(0.1f, blastRadius);
            lifeRemaining = Mathf.Max(0.5f, lifeTime);
            playerLayer = playerMask;
            armedTime = Time.time + Mathf.Max(0f, armDelay);
            Rigidbody.linearVelocity = velocity;
            Rigidbody.angularVelocity = Random.Range(-300f, 300f);
            previousPosition = Rigidbody.position;
        }

        private void Update()
        {
            if (exploded || IsHeld) return;
            lifeRemaining -= Time.deltaTime;
            if (lifeRemaining <= 0f) Destroy(gameObject);
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
                HandleContact(hit.collider);
                if (exploded) break;
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
            if (Time.time < armedTime) return;
            MoonBoss hitBoss = other.GetComponentInParent<MoonBoss>();
            if (hitBoss == owner && !isThrown) return;
            Explode();
        }

        private void Explode()
        {
            if (exploded) return;
            exploded = true;
            Rigidbody.linearVelocity = Vector2.zero;
            Rigidbody.simulated = false;

            if (explosionCaster != null)
            {
                explosionCaster.transform.position = transform.position;
                explosionCaster.ConfigureCircle(explosionRadius, playerLayer);
                explosionCaster.Cast(new DamageData(playerDamage, DamageType.Special));
                if (isThrown && owner != null)
                {
                    LayerMask bossLayer = 1 << owner.gameObject.layer;
                    explosionCaster.ConfigureCircle(explosionRadius, bossLayer);
                    explosionCaster.Cast(new DamageData(bossDamage, DamageType.Special));
                }
            }

            owner?.ShakeCamera(isThrown ? 0.78f : 0.62f);
            owner?.PlayRockExplosionFeedback(transform.position);

            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.45f, 0.15f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
