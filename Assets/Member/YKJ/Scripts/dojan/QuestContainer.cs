using KimLIb.EventSystem;
using UnityEngine;
using static QuestEvent;

public class QuestContainer : MonoBehaviour
{
    [SerializeField] private EventChannelSO QuestEventChannel;
    [SerializeField] private SelectQuestUI SelectQuestUI;

    private void Awake()
    {
        QuestEventChannel?.AddListener<SelectQuestEvent>(OnSelectQuestEvent);
    }

    private void OnDestroy()
    {
        QuestEventChannel?.RemoveListener<SelectQuestEvent>(OnSelectQuestEvent);
    }

    private void OnSelectQuestEvent(SelectQuestEvent evt)
    {
        SelectQuestUI?.SetData(evt.QuestData);
    }
}
