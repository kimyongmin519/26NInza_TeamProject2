using KimLIb.EventSystem;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class TimelineDialogSignal : MonoBehaviour
{
    [SerializeField] private PlayableDirector PlayableDirector;
    [SerializeField] private EventChannelSO DialogEventChannel;
    [SerializeField] private DialogDataSO DialogData;
    [SerializeField] private bool PlayTimelineOnStart;
    [SerializeField] private bool PauseTimelineUntilDialogEnd = true;

    private bool _waitingDialogEnd;
    private Coroutine _playDialogCoroutine;

    private void Awake()
    {
        if (PlayableDirector == null)
        {
            PlayableDirector = GetComponent<PlayableDirector>();
        }

        DialogEventChannel?.AddListener<EndDialogEvent>(OnEndDialogEvent);
    }

    private void Start()
    {
        if (PlayTimelineOnStart)
        {
            PlayTimeline();
        }
    }

    private void OnDestroy()
    {
        DialogEventChannel?.RemoveListener<EndDialogEvent>(OnEndDialogEvent);
    }

    public void SetDialogData(DialogDataSO dialogData) => DialogData = dialogData;

    public void PlayDialogSignal()
    {
        if (DialogEventChannel == null || DialogData == null)
        {
            Debug.LogWarning($"{nameof(TimelineDialogSignal)} needs DialogEventChannel and DialogData.", this);
            return;
        }

        if (PauseTimelineUntilDialogEnd)
        {
            PauseTimeline();
            _waitingDialogEnd = true;
        }

        if (_playDialogCoroutine != null)
        {
            StopCoroutine(_playDialogCoroutine);
        }

        _playDialogCoroutine = StartCoroutine(RaiseDialogNextFrame(DialogData));
    }

    public void PlayDialogSignal(DialogDataSO dialogData)
    {
        SetDialogData(dialogData);
        PlayDialogSignal();
    }

    public void PlayTimeline() => PlayableDirector?.Play();

    [ContextMenu("Play Dialog Signal")]
    private void PlayDialogSignalByInspector() => PlayDialogSignal();

    public void PauseTimeline()
    {
        if (PlayableDirector == null || !PlayableDirector.playableGraph.IsValid() || PlayableDirector.playableGraph.GetRootPlayableCount() == 0)
        {
            return;
        }

        PlayableDirector.playableGraph.GetRootPlayable(0).SetSpeed(0f);
    }

    public void ResumeTimeline()
    {
        if (PlayableDirector == null || !PlayableDirector.playableGraph.IsValid() || PlayableDirector.playableGraph.GetRootPlayableCount() == 0)
        {
            return;
        }

        PlayableDirector.playableGraph.GetRootPlayable(0).SetSpeed(1f);
    }

    public void EndTimeline()
    {
        if (PlayableDirector == null)
        {
            return;
        }

        PlayableDirector.time = PlayableDirector.duration;
    }

    private void OnEndDialogEvent(EndDialogEvent evt)
    {
        if (!_waitingDialogEnd)
        {
            return;
        }

        _waitingDialogEnd = false;
        ResumeTimeline();
    }

    private IEnumerator RaiseDialogNextFrame(DialogDataSO dialogData)
    {
        yield return null;

        DialogEventChannel.RaiseEvent(DialogEvent.StartDialogEvent.InitData(dialogData));
        _playDialogCoroutine = null;
    }
}
