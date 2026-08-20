using System;
using KimLIb.ModuleSystems;
using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.SkillSystems
{
    public interface ISkillModule
    {
        ModuleOwner Owner {get;}
        event Action OnCurrentSkillEnd;
        bool CanUseSkill(int skillIndex, GameObject target = null);
        void UseSkill(int skillIndex, GameObject target = null);
        ISkill GetCurrentSkill();
        void InvokeSkillEnd();
    }
}