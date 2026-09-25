using Member.KYM.Scripts.Players.RobotArm;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class LostSoulGrabbableProjectile : GrabbableRigidbody
    {
        [SerializeField] private float lifeTime = 9f;
        private LostSoul owner;
        private float playerDamage;
        private float bossDamage;
        private LayerMask playerLayer;
        private bool thrown;
        private bool consumed;

        public void Initialize(LostSoul boss, Vector2 velocity, float damageToPlayer, float damageToBoss, LayerMask targetLayer)
        {
            owner = boss;
            playerDamage = damageToPlayer;
            bossDamage = damageToBoss;
            playerLayer = targetLayer;
            Rigidbody.linearVelocity = velocity;
            Destroy(gameObject, lifeTime);
        }

        protected override void OnGrabbed() => thrown = false;
        protected override void OnReleased() => thrown = false;
        protected override void OnThrown(ThrowData throwData) => thrown = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed || IsHeld || other == null) return;
            if (thrown)
            {
                LostSoul boss = other.GetComponentInParent<LostSoul>();
                if (boss != null && boss == owner)
                {
                    boss.TakeDamage(new DamageData(bossDamage, DamageType.Projectile));
                    Consume();
                    return;
                }
            }

            int mask = 1 << other.gameObject.layer;
            if ((playerLayer.value & mask) == 0 && !other.transform.root.CompareTag("Player")) return;
            DamageCaster.ApplyDamage(other.transform, new DamageData(playerDamage, DamageType.Projectile));
            Consume();
        }

        private void Consume()
        {
            if (consumed) return;
            consumed = true;
            Destroy(gameObject);
        }
    }
}
