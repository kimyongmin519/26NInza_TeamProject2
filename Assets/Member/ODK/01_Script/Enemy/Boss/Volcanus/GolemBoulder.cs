using Member.KYM.Scripts.Players.RobotArm;
using Member.ODK.Scripts.Enemys.Bosses;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class GolemBoulder : GrabbableRigidbody
    {
        [SerializeField, Min(1f)] private float maximumThrownDistance = 18f;

        private Volcanus owner;
        private float bossDamage;
        private float playerDamage;
        private LayerMask playerLayer;
        private LayerMask groundLayer;
        private bool thrown;
        private bool consumed;
        private float armTime;
        private Vector2 previousPosition;
        private Vector2 thrownOrigin;

        public void Initialize(
            Volcanus boss,
            Vector2 velocity,
            float damageToBoss,
            float damageToPlayer,
            LayerMask playerMask,
            LayerMask groundMask)
        {
            owner = boss;
            bossDamage = Mathf.Clamp(damageToBoss, 0f, 1f);
            playerDamage = damageToPlayer;
            playerLayer = playerMask;
            groundLayer = groundMask;
            Rigidbody.linearVelocity = velocity;
            Rigidbody.angularVelocity = Random.Range(-260f, 260f);
            armTime = Time.time + 0.15f;
            previousPosition = Rigidbody.position;
            Destroy(gameObject, 12f);
        }

        private void FixedUpdate()
        {
            if (!thrown || consumed || IsHeld)
            {
                previousPosition = Rigidbody.position;
                return;
            }
            if (Vector2.Distance(thrownOrigin, Rigidbody.position) >= maximumThrownDistance)
            {
                Break();
                return;
            }
            Vector2 current = Rigidbody.position;
            foreach (RaycastHit2D hit in Physics2D.LinecastAll(previousPosition, current))
            {
                if (hit.collider == null || hit.rigidbody == Rigidbody) continue;
                if (TryHitBoss(hit.collider)) break;
            }
            previousPosition = current;
        }

        protected override void OnGrabbed() => thrown = false;
        protected override void OnReleased() => thrown = false;
        protected override void OnThrown(ThrowData throwData)
        {
            thrown = true;
            thrownOrigin = Rigidbody.position;
            armTime = Time.time;
            previousPosition = Rigidbody.position;
        }

        private void OnCollisionEnter2D(Collision2D collision) => HandleHit(collision.collider);
        private void OnTriggerEnter2D(Collider2D other) => HandleHit(other);
        private void OnTriggerStay2D(Collider2D other) => HandleHit(other);
        private void OnCollisionStay2D(Collision2D collision) => HandleHit(collision.collider);

        private void HandleHit(Collider2D other)
        {
            if (consumed || IsHeld || other == null) return;
            if (TryHitBoss(other) || Time.time < armTime) return;
            int mask = 1 << other.gameObject.layer;
            bool hitPlayer = (playerLayer.value & mask) != 0 || other.transform.root.CompareTag("Player");
            bool hitGround = (groundLayer.value & mask) != 0;
            if (hitPlayer)
            {
                if (!DamageCaster.IsWithinPlayerHitbox(GetComponent<Collider2D>(), other)) return;
                DamageCaster.ApplyDamage(other.transform, new DamageData(playerDamage, DamageType.Projectile));
                Break();
            }
            else if (hitGround) Break();
        }

        private bool TryHitBoss(Collider2D other)
        {
            if (!thrown || owner == null) return false;
            Volcanus hitBoss = other.GetComponentInParent<Volcanus>();
            if (hitBoss == null || hitBoss != owner) return false;
            hitBoss.TakeDamage(new DamageData(bossDamage, DamageType.Projectile));
            Break();
            return true;
        }

        private void Break()
        {
            if (consumed) return;
            consumed = true;
            owner?.PlayFeedback(VolcanusFeedbackType.Boulder, transform.position);
            owner?.ShakeCamera(thrown ? 0.7f : 0.52f);
            Destroy(gameObject);
        }
    }
}
