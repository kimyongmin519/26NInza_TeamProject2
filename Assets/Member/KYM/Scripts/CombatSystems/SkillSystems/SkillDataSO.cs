using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.SkillSystems
{
    [CreateAssetMenu(fileName = "Skill data", menuName = "KimSO/Skills/Skill data", order = 30)]
    public class SkillDataSO : ScriptableObject
    {
        [field:SerializeField] public int SkillIndex { get; private set; } 
        [field:TextArea]
        [field:SerializeField] public string SkillName { get; private set; }
        [field:SerializeField] public float Cooldown { get; private set; }
        public float skillRange = 1f;
        public float damageMultiplier = 1f;
    }
}