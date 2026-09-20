using KimLIb.ModuleSystems;
using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.Projectiles
{
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class AbstractProjectile : MonoBehaviour
    {
        [field:SerializeField] public ProjectileDataSO ProjectileData { get; private set; }
        public ModuleOwner Owner { get; private set; }

        protected Rigidbody2D Rigidbody { get; private set; }

        protected virtual void Awake()
        {
            Rigidbody = GetComponent<Rigidbody2D>();
        }

        public virtual void Shot(Vector2 direction)
        {
            Shot(direction, null);
        }

        public virtual void Shot(Vector2 direction, ModuleOwner owner)
        {
            Shot(direction, owner, ProjectileData.MoveSpeed);
        }

        public virtual void Shot(Vector2 direction, ModuleOwner owner, float launchSpeed)
        {
            Owner = owner;

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
