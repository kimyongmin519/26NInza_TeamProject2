using System;
using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.SkillSystems
{
    public interface ISkill
    {
        event Action OnSkillEnd;
        SkillDataSO SkillData { get; }
        bool IsUsing { get; }
        
        float NormalizedCooldown { get; } // 0~1로 표현되는 쿨다운 값 1이면 사용 가능
        
        void InitializeSkill(ISkillModule skillModule);
        bool CanUseSkill(GameObject target = null);
        void UseSkill(GameObject target = null);
        void StopSkill();
    }
}