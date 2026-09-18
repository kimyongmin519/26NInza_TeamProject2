using System.Collections.Generic;
using System.Linq;
using Member.Wst.Scripts.Achievements;
using Member.Wst.Scripts.Achievements.Datas;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    [SerializeField] private List<AchievementData> Quests = new();

    [Header("등급별 컨테이너 4개")]
    [SerializeField] private QuestRankContainer[] RankContainers = new QuestRankContainer[4];

    private Dictionary<AchievementRank, QuestRankContainer> _containerByRank;

    private void Awake()
    {
        _containerByRank = RankContainers
            .Where(container => container != null)
            .ToDictionary(container => container.QuestRank);
    }

    private void Start()
    {
        CreateQuestIcons();
    }

    public void CreateQuestIcons()
    {
        foreach (QuestRankContainer container in _containerByRank.Values)
            container.ClearIcons();

        foreach (AchievementData questData in Quests)
        {
            if (questData?.AchievementDataSO == null)
                continue;

            AchievementRank rank = questData.AchievementDataSO.AchievementRank;

            if (_containerByRank.TryGetValue(rank, out QuestRankContainer container))
                container.CreateQuestIcon(questData);
        }
    }
}
