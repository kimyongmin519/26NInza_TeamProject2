using Member.Wst.Scripts.Achievements;
using Member.Wst.Scripts.Achievements.Datas;
using TMPro;
using UnityEngine;

public class QuestRankContainer : MonoBehaviour
{
    [SerializeField] private AchievementRank Rank;
    [SerializeField] private TextMeshProUGUI RankText;
    [SerializeField] private Transform IconRoot;
    [SerializeField] private QuestIcon QuestIconPrefab;

    public AchievementRank QuestRank => Rank;

    private void Awake()
    {
        if (RankText != null)
            RankText.text = GetRankText(Rank);
    }

    public void ClearIcons()
    {
        if (IconRoot == null)
            return;

        for (int i = IconRoot.childCount - 1; i >= 0; i--)
            Destroy(IconRoot.GetChild(i).gameObject);
    }

    public void CreateQuestIcon(AchievementData questData)
    {
        if (questData?.AchievementDataSO == null || IconRoot == null || QuestIconPrefab == null)
            return;

        QuestIcon questIcon = Instantiate(QuestIconPrefab, IconRoot);
        questIcon.SetData(questData);
    }
    private void OnValidate()
    {
        RankText.text = GetRankText(Rank);
    }
    private static string GetRankText(AchievementRank rank)
    {
        return rank switch
        {
            AchievementRank.Normal => "일반",
            AchievementRank.Hard => "어려움",
            AchievementRank.Impossible => "불가능",
            AchievementRank.Secret => "비밀",
            _ => "알 수 없음"
        };
    }
}
