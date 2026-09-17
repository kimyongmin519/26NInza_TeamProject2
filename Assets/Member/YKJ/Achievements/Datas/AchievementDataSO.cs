using Member.Wst.Scripts.Achievements.Conditions;
using UnityEngine;

namespace Member.Wst.Scripts.Achievements.Datas
{
    [CreateAssetMenu(fileName = "AchieveData", menuName = "SO/achievement/Data", order = 0)]
    public class AchievementDataSO : ScriptableObject
    {
        [field: SerializeField] public AchievementType AchievementType { get; private set; }
        [field: SerializeField] public AchievementRank AchievementRank { get; private set; }
        [field: SerializeField] public Sprite AchievementIcon { get; private set; }
        [field: SerializeField] public string AchievementName { get; private set; }
        [field: TextArea]
        [field: SerializeField] public string AchievementDescription { get; private set; }
        [field: Min(1)]
        [field: SerializeField] public int TargetDegree { get; private set; } = 1;
        [field: SerializeField] public AchievementConditionSO Condition { get; private set; }
    }
}
