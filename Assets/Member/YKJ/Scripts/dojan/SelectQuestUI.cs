using Member.Wst.Scripts.Achievements;
using Member.Wst.Scripts.Achievements.Datas;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectQuestUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI QuestName;
    [SerializeField] private TextMeshProUGUI RankName;
    [SerializeField] private Image QuestIcon;
    [SerializeField] private TextMeshProUGUI Description;

    public void SetData(AchievementData questData)
    {
        if (questData?.AchievementDataSO == null)
            return;

        AchievementDataSO data = questData.AchievementDataSO;

        QuestName.text = data.AchievementName;
        RankName.text = ReplaceRankToText(data.AchievementRank);
        QuestIcon.sprite = data.AchievementIcon;
        Description.text = data.AchievementDescription;
    }

    private string ReplaceRankToText(AchievementRank rank)
    {
        switch (rank)
        {
            case AchievementRank.Normal:
                return "일반";
            case AchievementRank.Hard:
                return "어려움";
            case AchievementRank.Impossible:
                return "불가능";
            case AchievementRank.Secret:
                return "비밀";
            default:
                return "알 수 없음";
        }
    }
}
