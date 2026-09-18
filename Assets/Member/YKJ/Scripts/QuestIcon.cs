using KimLIb.EventSystem;
using Member.Wst.Scripts.Achievements.Datas;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class QuestIcon : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private EventChannelSO QuestEventChannel;
    [SerializeField] private Image Icon;

    [SerializeField] private AchievementData _questData;

    private void Awake()
    {
        SetData(_questData);
    }

    public void SetData(AchievementData questData)
    {
        _questData = questData;

        if (_questData?.AchievementDataSO != null)
            Icon.sprite = _questData.AchievementDataSO.AchievementIcon;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_questData == null)
            return;

        QuestEventChannel?.RaiseEvent(QuestEvent.SelectQuest.InitData(_questData));
    }
}
