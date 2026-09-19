using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class MoonFragment : MonoBehaviour
    {
        [SerializeField] private float defaultLifeTime = 5f;

        private Rigidbody2D body;
        private float damage;
        private float lifeRemaining;
        private bool consumed;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        public void Launch(Vector2 direction, float speed, float damageAmount, float lifeTime)
        {
            damage = Mathf.Max(0f, damageAmount);
            lifeRemaining = lifeTime > 0f ? lifeTime : defaultLifeTime;
            consumed = false;
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.linearVelocity = direction.normalized * speed;
            transform.right = direction;
        }

        private void Update()
        {
            lifeRemaining -= Time.deltaTime;
            if (lifeRemaining <= 0f) Destroy(gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryDamage(collision.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDamage(other);
        }

        private void TryDamage(Collider2D targetCollider)
        {
            if (consumed || targetCollider == null) return;
            if (!targetCollider.CompareTag("Player") &&
                !targetCollider.transform.root.CompareTag("Player"))
                return;

            consumed = true;
            DamageCaster.ApplyDamage(
                targetCollider.transform,
                new DamageData(damage, DamageType.Projectile)
            );
            Destroy(gameObject);
        }
    }
}
