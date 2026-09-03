using Member.KYM.Scripts.Agents;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Attacks
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "Member.ODK.Script", sourceAssembly: "Assembly-CSharp", sourceClassName: "IProjectile")]
    public interface IProjectile
    {
        void Launch(Agent ownerAgent, ProjectileData projectileData);
        void Release();
    }
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "Member.ODK.Script", sourceAssembly: "Assembly-CSharp", sourceClassName: "ProjectileData")]
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
