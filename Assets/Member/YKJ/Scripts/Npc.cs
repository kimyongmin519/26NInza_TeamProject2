using KimLIb.EventSystem;
using Member.KYM.Scripts.Agents;
using UnityEngine;

public class Npc : MonoBehaviour, Interactable
{
    [SerializeField] private EventChannelSO DialogEventChannel;
    [SerializeField] private EventChannelSO CameraEventChannel;
    [SerializeField] private Transform CameraFocusTarget;
    [SerializeField] private DialogDataSO dialogData;

    public void Interaction(Agent player)
    {
        if (DialogManager.Talking == false)
        {
            CameraEventChannel?.RaiseEvent(CameraEvent.FocusCameraTargetEvent.Init(CameraFocusTarget != null ? CameraFocusTarget : transform));
            DialogEventChannel.RaiseEvent(DialogEvent.StartDialogEvent.InitData(dialogData));
        }
    }
}

