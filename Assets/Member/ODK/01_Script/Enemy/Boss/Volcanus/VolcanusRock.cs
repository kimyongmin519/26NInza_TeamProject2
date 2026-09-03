using Member.KYM.Scripts.Players.RobotArm;
using System;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "VolcanusRock")]
    public class VolcanusRock : GrabbableRigidbody
    {
        public event Action<Vector3> OnBreak;

        private float damage;
        private float currentLifeTime;
        private bool isThrown;
        private bool isBroken;
        private Vector2 previousPosition;

        public void Setting(Vector2 startVelocity, float startAngularVelocity, float damage, float lifeTime)
        {
            this.damage = damage;
            currentLifeTime = lifeTime;
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
            if (hitPiece == null)
                return false;

            hitPiece.TakeDamage(new DamageData(damage, DamageType.Projectile));
            Break();
            return true;
        }

        private void Break()
        {
            if (isBroken)
                return;

            isBroken = true;
            OnBreak?.Invoke(transform.position);
            Destroy(gameObject);
        }
    }
}
