using KimLIb.SoundSystem;
using Member.KYM.Scripts.Players.RobotArm;
using Member.ODK.Scripts.Enemys.Bosses;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class LostSoulGrabbableProjectile : GrabbableRigidbody
    {
        [SerializeField] private float lifeTime = 9f;
        [SerializeField] private float homingDegreesPerSecond = 75f;
        [SerializeField] private float thrownHomingDegreesPerSecond = 150f;
        [SerializeField] private float visualAngleOffset = 180f;
        [SerializeField] private SoundClipSO impactSound;
        private LostSoul owner;
        private float bossDamage;
        private Transform target;
        private float speed;
        private bool thrown;
        private bool consumed;

        public void Initialize(LostSoul boss, Vector2 velocity, float damageToBoss, Transform homingTarget)
        {
            owner = boss;
            bossDamage = damageToBoss;
            target = homingTarget;
            speed = velocity.magnitude;
            Rigidbody.linearVelocity = velocity;
            SetVisualDirection(velocity);
            Destroy(gameObject, lifeTime);
        }

        private void FixedUpdate()
        {
            if (Rigidbody == null || target == null || IsHeld || consumed || speed <= 0.01f) return;
            Vector2 current = Rigidbody.linearVelocity.sqrMagnitude > 0.01f
                ? Rigidbody.linearVelocity.normalized
                : Vector2.right;
            Vector2 desired = ((Vector2)target.position - Rigidbody.position).normalized;
            float homing = thrown ? thrownHomingDegreesPerSecond : homingDegreesPerSecond;
            float radians = homing * Mathf.Deg2Rad * Time.fixedDeltaTime;
            Vector2 direction = Vector3.RotateTowards(current, desired, radians, 0f);
            Rigidbody.linearVelocity = direction.normalized * speed;
            SetVisualDirection(direction);
        }

        protected override void OnGrabbed() => thrown = false;
        protected override void OnReleased() => thrown = false;
        protected override void OnThrown(ThrowData throwData)
        {
            thrown = true;
            target = owner != null ? owner.transform : null;
            speed = Rigidbody != null ? Rigidbody.linearVelocity.magnitude : throwData.ArmThrowSpeed;
            SetVisualDirection(throwData.Direction);
        }

        private void SetVisualDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude <= 0.001f) return;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + visualAngleOffset;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed || IsHeld || other == null) return;
            if (thrown)
            {
                LostSoul boss = other.GetComponentInParent<LostSoul>();
                if (boss != null && boss == owner)
                {
                    boss.TakeDamage(new DamageData(bossDamage, DamageType.Projectile));
                    ODKSoundPlayback.Play(impactSound, transform.position);
                    Consume();
                    return;
                }
            }

            // The colored weak soul is intentionally harmless to the player.
            // It remains in play until grabbed and thrown back into its owner or until its lifetime ends.
        }

        private void Consume()
        {
            if (consumed) return;
            consumed = true;
            Destroy(gameObject);
        }
    }
}
