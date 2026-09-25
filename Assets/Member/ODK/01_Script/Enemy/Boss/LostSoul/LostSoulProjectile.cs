using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class LostSoulProjectile : MonoBehaviour
    {
        [SerializeField] private float lifeTime = 6f;
        private float damage;
        private LayerMask playerLayer;
        private bool consumed;

        public void Initialize(Vector2 velocity, float amount, LayerMask targetLayer)
        {
            damage = amount;
            playerLayer = targetLayer;
            Rigidbody2D body = GetComponent<Rigidbody2D>();
            body.linearVelocity = velocity;
            if (velocity.sqrMagnitude > 0.01f)
                transform.right = velocity.normalized;
            Destroy(gameObject, lifeTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed || other == null) return;
            int mask = 1 << other.gameObject.layer;
            if ((playerLayer.value & mask) == 0 && !other.transform.root.CompareTag("Player")) return;
            consumed = true;
            DamageCaster.ApplyDamage(other.transform, new DamageData(damage, DamageType.Projectile));
            Destroy(gameObject);
        }
    }
}
