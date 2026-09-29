using GGMLib.ObjectPool.Runtime;
using KimLIb.ObjectPool.Runtime;
using KimLIb.SoundSystem;
using Member.KYM.Scripts.CombatSystems.DamageSystems;
using Member.KYM.Scripts.Players.RobotArm;
using Member.ODK.Scripts.Enemys.Bosses;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class LostSoulGrabbableProjectile : GrabbableRigidbody, IPoolable, ICancellableBossSpawn
    {
        [SerializeField] private float lifeTime = 9f;
        [SerializeField] private float homingDegreesPerSecond = 75f;
        [SerializeField] private float thrownHomingDegreesPerSecond = 2160f;
        [SerializeField] private float visualAngleOffset = 180f;
        [SerializeField] private SoundClipSO impactSound;
        [SerializeField, Min(1f)] private float maximumThrownDistance = 18f;
        [SerializeField, Min(0f)] private float ownerHitPadding = 0.3f;
        private Collider2D[] ownerColliders;
        private LostSoul ownerCollidersOwner;
        private LostSoul owner;
        private float bossDamage;
        private Transform target;
        private float speed;
        private bool thrown;
        private bool consumed;
        private Vector2 thrownOrigin;
        private float lifeRemaining;
        private Collider2D projectileCollider;
        private float defaultGravityScale;
        private ODKGrabInfoDisplay grabInfoDisplay;

        public PoolItemSO PoolItem { get; set; }
        public GameObject GameObject => this != null ? gameObject : null;

        protected override void Awake()
        {
            base.Awake();
            projectileCollider = GetComponent<Collider2D>();
            grabInfoDisplay = GetComponent<ODKGrabInfoDisplay>();
            defaultGravityScale = Rigidbody.gravityScale;
        }

        public void ResetItem()
        {
            owner = null;
            target = null;
            thrown = false;
            consumed = false;
            speed = 0f;
            Rigidbody.simulated = true;
            Rigidbody.linearVelocity = Vector2.zero;
            Rigidbody.angularVelocity = 0f;
            Rigidbody.gravityScale = defaultGravityScale;
            if (projectileCollider != null) projectileCollider.enabled = true;
            grabInfoDisplay?.SetHeld(false);
        }

        public void Initialize(LostSoul boss, Vector2 velocity, float damageToBoss, Transform homingTarget)
        {
            owner = boss;
            bossDamage = Mathf.Clamp(damageToBoss, 0f, 1f);
            target = homingTarget;
            speed = velocity.magnitude;
            Rigidbody.linearVelocity = velocity;
            SetVisualDirection(velocity);
            thrown = false;
            consumed = false;
            lifeRemaining = lifeTime;
        }

        private void Update()
        {
            if (consumed || IsHeld) return;
            lifeRemaining -= Time.deltaTime;
            if (lifeRemaining <= 0f) Consume();
        }

        private void FixedUpdate()
        {
            if (thrown && !IsHeld && !consumed &&
                Vector2.Distance(thrownOrigin, Rigidbody.position) >= maximumThrownDistance)
            {
                Consume();
                return;
            }

            if (thrown && !IsHeld && !consumed && TryHitOwner()) return;

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

        protected override void OnGrabbed()
        {
            thrown = false;
            grabInfoDisplay?.SetHeld(true);
        }

        protected override void OnReleased()
        {
            thrown = false;
            grabInfoDisplay?.SetHeld(false);
        }

        protected override void OnThrown(ThrowData throwData)
        {
            thrown = true;
            grabInfoDisplay?.SetHeld(false);
            thrownOrigin = Rigidbody.position;
            target = owner != null ? owner.transform : null;
            speed = Rigidbody != null ? Rigidbody.linearVelocity.magnitude : throwData.ArmThrowSpeed;
            SetVisualDirection(throwData.Direction);
        }

        private bool TryHitOwner()
        {
            if (owner == null || owner.IsDead || projectileCollider == null) return false;
            if (ownerColliders == null || ownerCollidersOwner != owner)
            {
                ownerColliders = owner.GetComponents<Collider2D>();
                ownerCollidersOwner = owner;
            }
            foreach (Collider2D bossCollider in ownerColliders)
            {
                if (bossCollider == null || !bossCollider.enabled) continue;
                ColliderDistance2D distance = projectileCollider.Distance(bossCollider);
                if (!distance.isValid || distance.distance > ownerHitPadding) continue;
                HitOwner();
                return true;
            }
            return false;
        }

        private void HitOwner()
        {
            PlayerDamageFeedback.Apply(ThrowOwner, owner,
                new DamageData(bossDamage, DamageType.Projectile), transform.position);
            ODKSoundPlayback.Play(impactSound, transform.position);
            owner.PlayWeakSoulImpactFeedback(transform.position);
            Consume();
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
                    HitOwner();
                    return;
                }
            }

        }

        private void Consume()
        {
            if (consumed) return;
            consumed = true;
            grabInfoDisplay?.SetHeld(false);
            ODKPool.Despawn(this);
        }

        public void CancelBossSpawn() => Consume();
    }
}
