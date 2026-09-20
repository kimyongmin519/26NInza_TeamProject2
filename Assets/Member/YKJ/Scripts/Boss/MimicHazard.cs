using Member.ODK._01_Script;
using Member.ODK.Scripts;
using UnityEngine;

namespace Member.YKJ.Bosses
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class MimicHazard : MonoBehaviour
    {
        [SerializeField] private LayerMask playerLayers = 1 << 6;
        [SerializeField] private LayerMask groundLayers = (1 << 3) | (1 << 10);
        private MimicBoss _owner;
        private float _damage;
        private float _lifetime;
        private bool _spent;

        public void Launch(MimicBoss owner, Vector2 velocity, float gravityScale, float damage, float lifetime)
        {
            _owner = owner;
            _damage = damage;
            _lifetime = Mathf.Max(0.1f, lifetime);
            _spent = false;
            Rigidbody2D body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = gravityScale;
            body.linearDamping = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.linearVelocity = velocity;
            Collider2D collider = GetComponent<Collider2D>();
            if (owner == null)
                return;
            foreach (Collider2D other in owner.GetComponentsInChildren<Collider2D>())
                Physics2D.IgnoreCollision(collider, other, true);
        }

        private void Update()
        {
            _lifetime -= Time.deltaTime;
            if (_lifetime <= 0f)
                Retire();
        }

        private void OnCollisionEnter2D(Collision2D collision) => Impact(collision.collider);
        private void OnTriggerEnter2D(Collider2D other) => Impact(other);

        private void Impact(Collider2D other)
        {
            if (_spent || other == null || (_owner != null && other.transform.IsChildOf(_owner.transform)))
                return;
            if ((playerLayers.value & (1 << other.gameObject.layer)) != 0)
            {
                _spent = true;
                other.GetComponentInParent<IDamageable>()?.TakeDamage(new DamageData(_damage, DamageType.Projectile));
                Retire();
            }
            else if ((groundLayers.value & (1 << other.gameObject.layer)) != 0 && !other.isTrigger)
            {
                Retire();
            }
        }

        public void Retire()
        {
            _spent = true;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
