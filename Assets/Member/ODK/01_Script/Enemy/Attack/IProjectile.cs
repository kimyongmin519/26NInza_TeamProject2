using Member.KYM.Scripts.Agents;
using UnityEngine;

namespace Member.ODK.Script
{
    public interface IProjectile
    {
        void Launch(Agent ownerAgent, ProjectileData projectileData);
        void Release();
    }
    public readonly struct ProjectileData
    {
        public Vector2 Direction { get; }
        public float Damage { get; }
        public float Speed { get; }
        public float LifeTime { get; }

        public ProjectileData(
            Vector2 direction,
            float damage,
            float speed,
            float lifeTime)
        {
            Direction = direction.normalized;
            Damage = damage;
            Speed = speed;
            LifeTime = lifeTime;
        }
    }
}