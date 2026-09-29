using KimLIb.EventSystem;
using Member.KYM.Scripts.Agents;
using UnityEngine;
using UnityEngine.Events;

public class Npc : MonoBehaviour, Interactable
{
    [SerializeField] private EventChannelSO BubbleDialogEventChannel;
    [SerializeField] private EventChannelSO CameraEventChannel;
    [SerializeField] private Transform CameraFocusTarget;
    [SerializeField] private DialogDataSO dialogData;
    [SerializeField] private UnityEvent onDialogCompleted = new();
    private StartBubbleDialogEvent _request;

    public void Interaction(Agent player)
    {
        if (_request == null && DialogManager.Talking == false && BubbleDialogManager.Talking == false
            && dialogData != null && BubbleDialogEventChannel != null)
        {
            Transform target = CameraFocusTarget != null ? CameraFocusTarget : transform;
            var request = new StartBubbleDialogEvent().InitData(dialogData, target);
            request.Completed = () =>
            {
                if (_request != request) return;
                _request = null;
                if (this != null && isActiveAndEnabled && request.CompletedNormally)
                    onDialogCompleted?.Invoke();
            };
            _request = request;
            BubbleDialogEventChannel.RaiseEvent(request);
            if (request.Accepted)
                CameraEventChannel?.RaiseEvent(CameraEvent.FocusCameraTargetEvent.Init(target));
            else _request = null;
        }
    }

    private void OnDisable()
    {
        var request = _request;
        _request = null;
        if (request != null)
            BubbleDialogEventChannel?.RaiseEvent(new CancelBubbleDialogEvent { Request = request });
    }
}

