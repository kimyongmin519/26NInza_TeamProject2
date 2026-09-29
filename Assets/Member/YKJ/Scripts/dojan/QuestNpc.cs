using Member.KYM.Scripts.Agents;
using UnityEngine;

public sealed class QuestNpc : MonoBehaviour, Interactable
{
    [SerializeField] private QuestPanelController questPanel;

    public void Interaction(Agent player) => Open();

    public void Open()
    {
        if (DialogManager.Talking || BubbleDialogManager.Talking) return;
        questPanel?.Open();
    }
}
