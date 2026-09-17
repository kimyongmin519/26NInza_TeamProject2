using KimLIb.EventSystem;
using Member.Wst.Scripts.Achievements.Datas;

public static class QuestEvent
{
    public static readonly SelectQuestEvent SelectQuest = new();
    public static readonly GameStartedEvent GameStarted = new();

    public class GameStartedEvent : GameEvent
    {
    }

    public class SelectQuestEvent : GameEvent
    {
        public AchievementData QuestData;

        public SelectQuestEvent InitData(AchievementData questData)
        {
            QuestData = questData;
            return this;
        }
    }
}
