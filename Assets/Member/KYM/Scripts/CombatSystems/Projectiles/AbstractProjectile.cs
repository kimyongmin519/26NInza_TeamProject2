using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.Projectiles
{
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class AbstractProjectile : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float speed = 10f;

        public GameObject Owner { get; private set; }
        public float DamageMultiplier { get; private set; } = 1f;

        protected Rigidbody2D Rigidbody { get; private set; }

        protected virtual void Awake()
        {
            Rigidbody = GetComponent<Rigidbody2D>();
        }

        public virtual void Shot(Vector2 direction)
        {
            Shot(direction, null, speed, 1f);
        }

        public virtual void Shot(
            Vector2 direction,
            GameObject owner,
            float launchSpeed,
            float damageMultiplier = 1f)
        {
            Owner = owner;
            DamageMultiplier = Mathf.Max(0f, damageMultiplier);

            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                Rigidbody.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 normalizedDirection = direction.normalized;
            float rotationZ = Mathf.Atan2(
                normalizedDirection.y,
                normalizedDirection.x) * Mathf.Rad2Deg;

            transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
            Rigidbody.linearVelocity = normalizedDirection * Mathf.Max(0f, launchSpeed);
        }
    }
}
