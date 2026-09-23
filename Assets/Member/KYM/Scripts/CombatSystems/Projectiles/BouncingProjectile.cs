using KimLIb.ModuleSystems;
using UnityEngine;
using UnityEngine.Events;

namespace Member.KYM.Scripts.CombatSystems.Projectiles
{
    public class BouncingProjectile : GrabbableProjectile
    {
        [Header("벽 반사")]
        [SerializeField] private LayerMask bounceLayers = 1 << 3;
        [SerializeField, Min(1)] private int maxBounceCount = 3;
        [SerializeField, Range(0f, 2f)] private float bounceSpeedMultiplier = 1f;
        [SerializeField, Min(0f)] private float separationDistance = 0.02f;
        [SerializeField] private bool playImpactEffectOnBounce = true;

        [Header("반사 이벤트")]
        [SerializeField] private UnityEvent onBounce;

        public int BounceCount { get; private set; }

        public override void Shot(
            Vector2 direction,
            ModuleOwner owner,
            float launchSpeed)
        {
            BounceCount = 0;
            base.Shot(direction, owner, launchSpeed);
        }

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            if (CanBounce(other) && TryBounce(other))
                return;

            base.OnTriggerEnter2D(other);
        }

        private bool CanBounce(Collider2D other)
        {
            if (IsHeld || other == null || BounceCount >= maxBounceCount)
                return false;

            int otherLayerMask = 1 << other.gameObject.layer;
            return (bounceLayers.value & otherLayerMask) != 0;
        }

        private bool TryBounce(Collider2D wall)
        {
            Vector2 incomingVelocity = Rigidbody.linearVelocity;
            if (incomingVelocity.sqrMagnitude <= Mathf.Epsilon)
                return false;

            GetImpactContact(
                wall,
                out Vector2 hitPoint,
                out Vector2 hitNormal);

            Vector2 normal = hitNormal.sqrMagnitude > Mathf.Epsilon
                ? hitNormal.normalized
                : -incomingVelocity.normalized;
            Vector2 reflectedDirection = Vector2
                .Reflect(incomingVelocity.normalized, normal)
                .normalized;

            if (reflectedDirection.sqrMagnitude <= Mathf.Epsilon)
                return false;

            float reflectedSpeed =
                incomingVelocity.magnitude * bounceSpeedMultiplier;
            if (reflectedSpeed <= Mathf.Epsilon)
                return false;

            BounceCount++;

            Rigidbody.position = hitPoint +
                reflectedDirection * separationDistance;
            Rigidbody.linearVelocity = reflectedDirection * reflectedSpeed;

            float rotationZ = Mathf.Atan2(
                reflectedDirection.y,
                reflectedDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);

            if (playImpactEffectOnBounce)
                PlayImpactEffect(hitPoint, normal);

            onBounce?.Invoke();
            return true;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            maxBounceCount = Mathf.Max(1, maxBounceCount);
            bounceSpeedMultiplier = Mathf.Max(0f, bounceSpeedMultiplier);
            separationDistance = Mathf.Max(0f, separationDistance);
        }
    }
}
