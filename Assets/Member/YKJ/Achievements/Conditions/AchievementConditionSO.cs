using Member.Wst.Scripts.Achievements.Datas;
using UnityEngine;

namespace Member.Wst.Scripts.Achievements.Conditions
{
    public interface IAchievementCondition
    {
        void Bind(AchievementData data);
        void Unbind();
    }

    public abstract class AchievementConditionSO : ScriptableObject
    {
        public abstract IAchievementCondition Create();
    }
}
