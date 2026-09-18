using System;

namespace Member.Wst.Scripts.Achievements.Saves
{
    [Serializable]
    public class AchieveSaveData
    {
        public AchievementType achievementType;
        public int nowAchievementDegree;
        public bool isComplete;
    }
}
