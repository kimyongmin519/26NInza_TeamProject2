using KimLIb.EventSystem;
using Member.Wst.Scripts.Achievements.Datas;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class QuestIcon : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private EventChannelSO QuestEventChannel;
    [SerializeField] private Image Icon;
    [SerializeField] private Image LockIcon;
    [SerializeField] private bool IsLock;
    [SerializeField] private AchievementData _questData;

    private void Awake()
    {
        SetData(_questData);
    }

    public void SetData(AchievementData questData)
    {
        if (_questData != null)
            _questData.OnChanged -= RefreshLock;
        _questData = questData;

        if (_questData?.AchievementDataSO != null)
            Icon.sprite = _questData.AchievementDataSO.AchievementIcon;
        if (_questData != null)
            _questData.OnChanged += RefreshLock;
        RefreshLock(_questData);
    }

    private void RefreshLock(AchievementData data)
    {
        IsLock = data == null || !data.AchieveSaveData.isComplete;
        if (LockIcon != null)
            LockIcon.gameObject.SetActive(IsLock);
    }

    private void OnDestroy()
    {
        if (_questData != null)
            _questData.OnChanged -= RefreshLock;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_questData == null)
            return;

        QuestEventChannel?.RaiseEvent(QuestEvent.SelectQuest.InitData(_questData));
    }
}
