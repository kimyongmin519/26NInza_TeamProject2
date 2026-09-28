using System;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public class VolcanusMissile : MonoBehaviour
    {
        [SerializeField] private float turnSpeed = 150f;
        [SerializeField] private float impactRadius = 1.2f;
        [SerializeField] private DamageCaster damageCaster;

        public event Action<Vector3> OnExplode;

        private Transform target;
        private Vector2 velocity;
        private float acceleration;
        private float maxSpeed;
        private float remainingLife;
        private float damage;
        private LayerMask playerLayer;
        private bool launched;

        public void Launch(
            Transform target,
            float startSpeed,
            float acceleration,
            float maxSpeed,
            float lifeTime,
            float damage,
            LayerMask playerLayer)
        {
            this.target = target;
            this.acceleration = acceleration;
            this.maxSpeed = maxSpeed;
            remainingLife = lifeTime;
            this.damage = damage;
            this.playerLayer = playerLayer;
            if (damageCaster == null) damageCaster = GetComponent<DamageCaster>();
            if (damageCaster == null)
            {
                Debug.LogError("Volcanus Missile DamageCaster is not connected.", this);
                Destroy(gameObject);
                return;
            }
            damageCaster.ConfigureCircle(impactRadius, playerLayer);
            Vector2 direction = target != null
                ? ((Vector2)target.position - (Vector2)transform.position).normalized
                : Vector2.down;
            velocity = direction * startSpeed;
            launched = true;
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
        }

        private void Update()
        {
            if (!launched) return;
            if (target != null)
            {
                Vector2 desired = ((Vector2)target.position - (Vector2)transform.position).normalized;
                Vector2 current = velocity.sqrMagnitude > 0.001f ? velocity.normalized : desired;
                float maximumRadians = turnSpeed * Mathf.Deg2Rad * Time.deltaTime;
                Vector2 steered = Vector3.RotateTowards(current, desired, maximumRadians, 0f);
                float speed = Mathf.MoveTowards(velocity.magnitude, maxSpeed, acceleration * Time.deltaTime);
                velocity = steered.normalized * speed;
            }

            transform.position += (Vector3)(velocity * Time.deltaTime);
            if (velocity.sqrMagnitude > 0.001f)
                transform.right = velocity.normalized;

            damageCaster.SetWorldPosition(transform.position);
            if (damageCaster.Cast(new DamageData(damage, DamageType.Projectile)))
            {
                Explode();
                return;
            }

            remainingLife -= Time.deltaTime;
            if (remainingLife <= 0f) Explode();
        }

        public void Explode()
        {
            if (!launched) return;
            launched = false;
            transform.DOKill();
            OnExplode?.Invoke(transform.position);
            Destroy(gameObject);
        }

        private void OnDestroy() => transform.DOKill();
    }
}
