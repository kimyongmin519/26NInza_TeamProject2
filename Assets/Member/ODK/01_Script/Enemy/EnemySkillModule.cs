using System;
using System.Collections.Generic;
using System.Linq;
using KimLIb.ModuleSystems;
using Member.KYM.Scripts.CombatSystems.SkillSystems;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys
{
    public class EnemySkillModule : MonoBehaviour, IModule, ISkillModule
    {
        public ModuleOwner Owner { get; private set; }
        public event Action OnCurrentSkillEnd;

        private Dictionary<int, ISkill> _skillDict;
        private ISkill _currentSkill;
        public void Initialize(ModuleOwner owner)
        {
            Owner = owner;

            _skillDict = new Dictionary<int, ISkill>();
            foreach (ISkill skill in GetComponentsInChildren<ISkill>())
            {
                if (skill.SkillData == null)
                {
                    Debug.LogError($"[{name}] SkillData is missing on {skill}.", this);
                    continue;
                }

                int index = skill.SkillData.SkillIndex;
                if (!_skillDict.TryAdd(index, skill))
                {
                    Debug.LogError($"[{name}] Duplicate skill index: {index}", this);
                    continue;
                }
            }

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
        public ISkill[] GetAllSkill()
        {
            return _skillDict == null
                ? Array.Empty<ISkill>()
                : _skillDict.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToArray();
        }
        public ISkill GetSkill(int skillIndex) =>
            _skillDict != null && _skillDict.TryGetValue(skillIndex, out ISkill skill)
                ? skill
                : null;
        public ISkill GetCurrentSkill() => _currentSkill;
        public void InvokeSkillEnd() => OnCurrentSkillEnd?.Invoke();
    }
}
