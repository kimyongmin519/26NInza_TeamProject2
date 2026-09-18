using System;
using System.Collections.Generic;
using System.Linq;
using KimLIb.ModuleSystems;
using Member.KYM.Scripts.CombatSystems.SkillSystems;
using UnityEngine;

namespace Member.KYM.Scripts.Players
{
    public class PlayerSkillModule : MonoBehaviour, IModule, ISkillModule
    {
        public ModuleOwner Owner { get; private set; }
        public event Action OnCurrentSkillEnd;
        
        private Dictionary<int, ISkill> _skillDict;
        private ISkill _currentSkill;
        public void Initialize(ModuleOwner owner)
        {
            Owner = owner;
            
            _skillDict = GetComponentsInChildren<ISkill>()
                .ToDictionary(skill => skill.SkillData.SkillIndex, skill => skill);
            foreach (ISkill skill in _skillDict.Values)
            {
                skill.InitializeSkill(this);
            }
        }

        public bool CanUseSkill(int skillIndex, GameObject target = null)
        {
            if (_currentSkill is { IsUsing: true })
                return false;

            if (_skillDict.TryGetValue(skillIndex, out ISkill skill))
            {
                return skill.CanUseSkill(target);
            }

            return false;
        }

        public void UseSkill(int skillIndex, GameObject target = null)
        {
            if (_skillDict.TryGetValue(skillIndex, out ISkill skill))
            {
                if (_currentSkill != null)
                    _currentSkill.OnSkillEnd -= HandleCurrentSkillEnd;
                _currentSkill = skill;
                _currentSkill.OnSkillEnd += HandleCurrentSkillEnd;
                _currentSkill.UseSkill(target);
            }
        }
        
        private void HandleCurrentSkillEnd()
        {
            InvokeSkillEnd();

            _currentSkill.OnSkillEnd -= HandleCurrentSkillEnd;
        }

        public ISkill GetCurrentSkill() => _currentSkill;
        public void InvokeSkillEnd() => OnCurrentSkillEnd?.Invoke();
    }
}