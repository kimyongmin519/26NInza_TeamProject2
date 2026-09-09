
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Skills
{
    public class FireBullets : AbstractEnemySkill
    {
        
        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);

        }
        public override void UseSkill(SkillCommand command, GameObject target = null)
        {
            base.UseSkill(command, target);

        }
        public override bool CanUseSkill(GameObject target = null)
        {
            return true;
        }
    }
}
