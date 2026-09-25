using KimLIb.EventSystem;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class BubbleDialogManager : MonoBehaviour
{
    [SerializeField] private EventChannelSO BubbleDialogEventChannel;
    [SerializeField] private EventChannelSO InputChannel;
    [SerializeField] private EventChannelSO CameraEventChannel;
    [SerializeField] private float AutoNextDelay = 1.5f;
    [SerializeField] private float TypingFallbackTimeout = 10f;
    [SerializeField] private Key AdvanceKey = Key.E;
    [SerializeField] private bool LockInputDuringDialog = true;

    public static bool Talking { get; private set; }

    private Coroutine _dialogCoroutine;
    private StartBubbleDialogEvent _activeRequest;
    private System.Action _completed;

    private void Awake()
    {
        BubbleDialogEventChannel?.AddListener<StartBubbleDialogEvent>(HandleStartDialog);
        BubbleDialogEventChannel?.AddListener<CancelBubbleDialogEvent>(HandleCancelDialog);
    }

    private void OnDestroy()
    {
        BubbleDialogEventChannel?.RemoveListener<StartBubbleDialogEvent>(HandleStartDialog);
        BubbleDialogEventChannel?.RemoveListener<CancelBubbleDialogEvent>(HandleCancelDialog);

        EndDialog();
    }

    private void HandleStartDialog(StartBubbleDialogEvent evt)
    {
        if (!isActiveAndEnabled || evt.DialogData == null)
        {
            return;
        }

        if (_dialogCoroutine != null)
        {
            EndDialog();
        }

        _activeRequest = evt;
        _completed = evt.Completed;
        evt.Accepted = true;
        Talking = true;

        if (LockInputDuringDialog)
        {
            InputChannel?.RaiseEvent(InputEvent.LockInputAllEvent.Init(true));
        }

        _dialogCoroutine = StartCoroutine(DialogCoroutine(evt.DialogData, evt.Target));
    }

    private IEnumerator DialogCoroutine(DialogDataSO dialogData, Transform target)
    {
        yield return null;

        if (dialogData.DialogList == null)
        {
            EndDialog();
            yield break;
        }

        for (int i = 0; i < dialogData.DialogList.Count; i++)
        {
            DiglogData dialog = dialogData.DialogList[i];
            bool isTyping = true;
            float typingTime = 0f;

            BubbleDialogEventChannel?.RaiseEvent(BubbleDialogEvent.SetBubbleDialogLineEvent.InitData(
                dialog.Id,
                dialog.SpeakerName,
                dialog.SpeakeIcon,
                dialog.Description,
                target,
                () => isTyping = false));

            yield return null;

            while (isTyping)
            {
                if (WasAdvancePressed())
                {
                    BubbleDialogEventChannel?.RaiseEvent(BubbleDialogEvent.SkipBubbleDialogLineEvent);
                    yield return WaitUntilTypingEndOrTimeout(() => isTyping == false, 0.2f);
                    isTyping = false;
                    break;
                }

                typingTime += Time.deltaTime;
                if (TypingFallbackTimeout > 0f && typingTime >= TypingFallbackTimeout)
                {
                    isTyping = false;
                    break;
                }

                yield return null;
            }

            float currentTime = 0f;
            while (currentTime < AutoNextDelay)
            {
                if (WasAdvancePressed())
                {
                    break;
                }

                currentTime += Time.deltaTime;
                yield return null;
            }
        }

        EndDialog();
    }

    private void EndDialog()
    {
        if (_activeRequest == null)
            return;
        System.Action completed = _completed;
        _completed = null;
        _activeRequest = null;
        if (_dialogCoroutine != null)
            StopCoroutine(_dialogCoroutine);
        _dialogCoroutine = null;
        Talking = false;

        if (LockInputDuringDialog)
        {
            InputChannel?.RaiseEvent(InputEvent.LockInputAllEvent.Init(false));
        }

        CameraEventChannel?.RaiseEvent(CameraEvent.ReturnDefaultCameraTargetEvent);
        BubbleDialogEventChannel?.RaiseEvent(BubbleDialogEvent.EndBubbleDialogEvent);
        completed?.Invoke();
    }

    private void HandleCancelDialog(CancelBubbleDialogEvent evt)
    {
        if (ReferenceEquals(evt.Request, _activeRequest))
            EndDialog();
    }

    private void OnDisable() => EndDialog();

    private bool WasAdvancePressed()
    {
        if (AdvanceKey == Key.None || Keyboard.current == null)
        {
            return false;
        }

        return Keyboard.current[AdvanceKey].wasPressedThisFrame;
    }

    private IEnumerator WaitUntilTypingEndOrTimeout(System.Func<bool> predicate, float timeout)
    {
        float currentTime = 0f;

        while (predicate.Invoke() == false && currentTime < timeout)
        {
            currentTime += Time.deltaTime;
            yield return null;
        }
    }
}
