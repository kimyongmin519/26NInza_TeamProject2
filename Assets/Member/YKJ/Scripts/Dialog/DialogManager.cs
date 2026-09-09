using KimLIb.EventSystem;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogManager : MonoBehaviour
{
    [SerializeField] private EventChannelSO DialogEventChannel;
    [SerializeField] private EventChannelSO UIEventChannel;
    [SerializeField] private EventChannelSO InputChannel;
    [SerializeField] private EventChannelSO CameraEventChannel;



    public static bool Talking { get; private set; } = false;
    private Coroutine _dialogCoroutine;
    private void Awake()
    {
        DialogEventChannel.AddListener<StartDialogEvent>(HandleStartDialog);
    }
    private void OnDestroy()
    {
        DialogEventChannel.RemoveListener<StartDialogEvent>(HandleStartDialog);
    }

    private void HandleStartDialog(StartDialogEvent startDialogEvent)
    {
        if (_dialogCoroutine != null)
        {
            StopCoroutine(_dialogCoroutine);
        }

        Talking = true;
        InputChannel.RaiseEvent(InputEvent.LockInputAllEvent.Init(true));
        UIEventChannel.RaiseEvent(UiEvent.HighlightEvent.Init(true));
        _dialogCoroutine = StartCoroutine(DialogCorutine(startDialogEvent.dialogData));
    }
    private IEnumerator DialogCorutine(DialogDataSO dialogData)
    {
        int currentDialogIndex = 0;
        yield return null;
        while (currentDialogIndex < dialogData.DialogList.Count)
        {
            Debug.Log("���� ����");
            int SpeakerId = dialogData.DialogList[currentDialogIndex].Id;
            string SpeakerName = dialogData.DialogList[currentDialogIndex].SpeakerName;
            Sprite SpeakerIcon = dialogData.DialogList[currentDialogIndex].SpeakeIcon;
            string Description = dialogData.DialogList[currentDialogIndex].Description;
            bool isTyping = true;
            DialogEventChannel.RaiseEvent(DialogEvent.SetDialogLineEvent.InitData(SpeakerId, SpeakerName, SpeakerIcon, Description, () => isTyping = false));
            yield return null;

            yield return new WaitUntil(() => Keyboard.current.eKey.wasPressedThisFrame);

            if (isTyping)
            {
                DialogEventChannel.RaiseEvent(DialogEvent.SkipDialogLineEvent);
                yield return new WaitUntil(() => isTyping == false);
                yield return null;
                yield return new WaitUntil(() => Keyboard.current.eKey.wasPressedThisFrame);
            }
            currentDialogIndex++;
        }
        Talking = false;
        UIEventChannel.RaiseEvent(UiEvent.HighlightEvent.Init(false));
        InputChannel.RaiseEvent(InputEvent.LockInputAllEvent.Init(false));
        CameraEventChannel?.RaiseEvent(CameraEvent.ReturnDefaultCameraTargetEvent);
        DialogEventChannel.RaiseEvent(DialogEvent.EndDialogEvent);
        _dialogCoroutine = null;
    }
}
