using GGMLib.ObjectPool.Runtime;
using KimLIb.ObjectPool.Runtime;
using Member.KYM.Scripts.Players.RobotArm;
using Member.ODK.Scripts.Enemys.Bosses;
using System;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus.Legacy
{
    public class VolcanusRock : GrabbableRigidbody, IPoolable
    {
        public event Action<Vector3> OnBreak;

        private float damage;
        private float currentLifeTime;
        private bool isThrown;
        private bool isBroken;
        private Vector2 previousPosition;

        public PoolItemSO PoolItem { get; set; }
        public GameObject GameObject => this != null ? gameObject : null;

        public void ResetItem()
        {
            OnBreak = null;
            isThrown = false;
            isBroken = false;
            Rigidbody.bodyType = RigidbodyType2D.Dynamic;
            Rigidbody.simulated = true;
            Rigidbody.linearVelocity = Vector2.zero;
            Rigidbody.angularVelocity = 0f;
        }

        public void Setting(Vector2 startVelocity, float startAngularVelocity, float damage, float lifeTime)
        {
            this.damage = damage;
            currentLifeTime = lifeTime;
            isThrown = false;
            isBroken = false;
            Rigidbody.bodyType = RigidbodyType2D.Dynamic;
            Rigidbody.simulated = true;
            Rigidbody.WakeUp();
            Rigidbody.linearVelocity = startVelocity;
            Rigidbody.angularVelocity = startAngularVelocity;
            previousPosition = Rigidbody.position;
        }

        private void Update()
        {
            if (IsHeld || isBroken)
                return;

            currentLifeTime -= Time.deltaTime;
            if (currentLifeTime <= 0f)
                Break();
        }

        private void FixedUpdate()
        {
            if (!isThrown || isBroken)
            {
                previousPosition = Rigidbody.position;
                return;
            }

            Vector2 currentPosition = Rigidbody.position;
            Vector2 direction = currentPosition - previousPosition;
            if (direction.sqrMagnitude > 0.0001f)
            {
                RaycastHit2D[] hits = Physics2D.LinecastAll(previousPosition, currentPosition);
                foreach (RaycastHit2D hit in hits)
                {
                    if (hit.collider == null || hit.rigidbody == Rigidbody)
                        continue;

                    if (Attack(hit.collider))
                        break;
                }
            }

            previousPosition = currentPosition;
        }

        protected override void OnGrabbed()
        {
            isThrown = false;
            previousPosition = Rigidbody.position;
        }

        protected override void OnReleased()
        {
            isThrown = false;
            previousPosition = Rigidbody.position;
        }

        protected override void OnThrown(ThrowData throwData)
        {
            isThrown = true;
            previousPosition = Rigidbody.position;
            Rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            Attack(collision.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Attack(other);
        }

        private bool Attack(Collider2D targetCollider)
        {
            if (!isThrown || isBroken)
                return false;

            VolcanusPiece hitPiece = targetCollider.GetComponentInParent<VolcanusPiece>();
            if (hitPiece != null)
            {
                hitPiece.TakeDamage(new DamageData(damage, DamageType.Projectile));
                Break();
                return true;
            }

            global::Member.ODK.Scripts.Enemys.Volcanus.Volcanus activeBoss =
                targetCollider.GetComponentInParent<global::Member.ODK.Scripts.Enemys.Volcanus.Volcanus>();
            if (activeBoss == null)
                return false;

            activeBoss.TakeDamage(new DamageData(damage, DamageType.Projectile));
            Break();
            return true;
        }

        private void Break()
        {
            if (isBroken)
                return;

            isBroken = true;
            OnBreak?.Invoke(transform.position);
            OnBreak = null;
            ODKPool.Despawn(this);
        }
    }
}
