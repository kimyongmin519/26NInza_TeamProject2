using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public class GolemGroundWave : MonoBehaviour
    {
        [SerializeField] private DamageCaster caster;
        private float speed;
        private float lifeRemaining;
        private Vector2 direction;

        public void Initialize(
            Vector2 moveDirection,
            float moveSpeed,
            float damage,
            float lifeTime,
            LayerMask playerLayer)
        {
            direction = moveDirection.normalized;
            speed = moveSpeed;
            lifeRemaining = lifeTime;
            if (caster == null) caster = GetComponentInChildren<DamageCaster>(true);
            if (caster == null)
            {
                Destroy(gameObject);
                return;
            }
            caster.ConfigureBox(new Vector2(1.5f, 1.1f), playerLayer);
            caster.EnableCasting(new DamageData(damage, DamageType.Special), lifeTime);
        }

        private void Update()
        {
            transform.position += (Vector3)direction * speed * Time.deltaTime;
            lifeRemaining -= Time.deltaTime;
            if (lifeRemaining <= 0f) Destroy(gameObject);
        }
    }
}
