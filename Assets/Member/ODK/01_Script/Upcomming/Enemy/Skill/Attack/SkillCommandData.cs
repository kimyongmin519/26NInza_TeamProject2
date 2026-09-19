using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Skills
{
    public struct SkillCommand
    {
        public int skillIndex;
        public Vector2 direction;

        public int projectileCount;
        public float spreadAngle;
        public float projectileSpeed;

        public int repeatCount;
        public float shotInterval;

        public SkillCommand(
            int skillIndex,
            Vector2 direction,
            int projectileCount,
            float spreadAngle,
            float projectileSpeed,
            int repeatCount,
            float shotInterval)
        {
            this.skillIndex = skillIndex;
            this.direction = direction;

            this.projectileCount = projectileCount;
            this.spreadAngle = spreadAngle;
            this.projectileSpeed = projectileSpeed;

            this.repeatCount = repeatCount;
            this.shotInterval = shotInterval;
        }
    }
}
