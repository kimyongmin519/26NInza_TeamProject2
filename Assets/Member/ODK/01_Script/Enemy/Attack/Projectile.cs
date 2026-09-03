using GGMLib.ObjectPool.Runtime;
using KimLIb.ObjectPool.Runtime;
using Member.KYM.Scripts.Agents;
using UnityEngine;

namespace Member.ODK.Script
{
    public class Projectile : MonoBehaviour, IProjectile, IPoolable
    {
        private Agent ownerAgent;
        private ProjectileData projectileData;

        private float currentLifeTime;
        private bool isLaunched;

        public Agent OwnerAgent => ownerAgent;
        public float Damage => projectileData.Damage;

        public PoolItemSO PoolItem { get; set; }

        public GameObject GameObject => gameObject;

        public void Launch(Agent ownerAgent, ProjectileData projectileData)
        {
            this.ownerAgent = ownerAgent;
            this.projectileData = projectileData;

            currentLifeTime = projectileData.LifeTime;
            isLaunched = true;

            RotateToDirection();
        }

        private void Update()
        {
            if (!isLaunched)
                return;

            Move();
            UpdateLifeTime();
        }

        private void Move()
        {
            transform.position +=
                (Vector3)(projectileData.Direction *
                          projectileData.Speed *
                          Time.deltaTime);
        }

        private void UpdateLifeTime()
        {
            currentLifeTime -= Time.deltaTime;

            if (currentLifeTime <= 0f)
            {
                Release();
            }
        }

        private void RotateToDirection()
        {
            Vector2 direction = projectileData.Direction;

            if (direction == Vector2.zero)
                return;

            float angle = Mathf.Atan2(direction.y, direction.x)
                          * Mathf.Rad2Deg;

            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void Release()
        {
            isLaunched = false;
            ownerAgent = null;

            Destroy(gameObject);
        }

        public void ResetItem()
        {
            throw new System.NotImplementedException();
        }
    }
}