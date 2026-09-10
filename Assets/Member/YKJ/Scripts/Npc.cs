using KimLIb.EventSystem;
using Member.KYM.Scripts.Agents;
using UnityEngine;

public class Npc : MonoBehaviour, Interactable
{
    [SerializeField] private EventChannelSO BubbleDialogEventChannel;
    [SerializeField] private EventChannelSO CameraEventChannel;
    [SerializeField] private Transform CameraFocusTarget;
    [SerializeField] private DialogDataSO dialogData;

    public void Interaction(Agent player)
    {
        if (DialogManager.Talking == false)
        {
            Transform target = CameraFocusTarget != null ? CameraFocusTarget : transform;
            CameraEventChannel?.RaiseEvent(CameraEvent.FocusCameraTargetEvent.Init(target));
            BubbleDialogEventChannel?.RaiseEvent(BubbleDialogEvent.StartBubbleDialogEvent.InitData(dialogData, target));
        }
    }
}

