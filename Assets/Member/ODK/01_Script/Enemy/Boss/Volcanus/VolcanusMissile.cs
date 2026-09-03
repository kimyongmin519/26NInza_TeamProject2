using DG.Tweening;
using System;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public class VolcanusMissile : MonoBehaviour
    {
        [SerializeField] private float rotateSpeed = 720f;
        [SerializeField] private GameObject explodeEffect;
        [SerializeField] private DamageCaster damageCaster;
        public event Action<Vector3> OnExplode;

        private Transform target;
        private Vector2 velocity;
        private float acceleration;
        private float maxSpeed;
        private float lifeTime;
        private float damage;
        private bool isLaunched;

        public void Launch(Transform target, float startSpeed, float acceleration, float maxSpeed, float lifeTime, float damage)
        {
            this.target = target;
            this.acceleration = acceleration;
            this.maxSpeed = maxSpeed;
            this.lifeTime = lifeTime;
            this.damage = damage;
            if (damageCaster == null) damageCaster = GetComponent<DamageCaster>();
            if (damageCaster == null)
            {
                damageCaster = gameObject.AddComponent<DamageCaster>();
                damageCaster.SetRange(1.2f);
            }
            Vector2 direction = target != null ? ((Vector2)target.position - (Vector2)transform.position).normalized : Vector2.down;
            velocity = direction * startSpeed;
            isLaunched = true;
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
        }

        private void Update()
        {
            if (!isLaunched) return;

            if (target != null)
            {
                Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
                velocity = Vector2.MoveTowards(velocity, direction * maxSpeed, acceleration * Time.deltaTime);
                if (ApplyDamage())
                {
                    Explode();
                    return;
                }
            }

            transform.position += (Vector3)(velocity * Time.deltaTime);
            RotateToVelocity();
            lifeTime -= Time.deltaTime;
            if (lifeTime <= 0f) Explode();
        }

        private void RotateToVelocity()
        {
            if (velocity.sqrMagnitude <= Mathf.Epsilon) return;
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0f, 0f, angle), rotateSpeed * Time.deltaTime);
        }

        private bool ApplyDamage()
        {
            if (target == null) return false;
            DamageData damageData = new DamageData(damage, DamageType.Projectile);
            return damageCaster != null
                ? damageCaster.Cast(damageData)
                : DamageCaster.ApplyDamage(target, damageData);
        }

        public void Explode()
        {
            if (!isLaunched) return;
            isLaunched = false;
            transform.DOKill();
            OnExplode?.Invoke(transform.position);
            if (explodeEffect != null) Instantiate(explodeEffect, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }

        private void OnDestroy() => transform.DOKill();

    }
}
