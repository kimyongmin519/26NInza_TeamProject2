using KimLIb.EventSystem;
using Member.KYM.Scripts.CoreSystems.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
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
    private IDisposable _timePause;
    private readonly List<Playable> _pausedRoots = new();
    private readonly List<double> _previousSpeeds = new();

    private void Awake()
    {
        if (PlayableDirector == null)
        {
            PlayableDirector = GetComponent<PlayableDirector>();
        }
    }

    private void OnEnable()
    {
        DialogEventChannel?.AddListener<EndDialogEvent>(OnEndDialogEvent);
        if (PlayableDirector != null) PlayableDirector.stopped += HandleDirectorStopped;
    }

    private void Start()
    {
        if (PlayTimelineOnStart)
        {
            PlayTimeline();
        }
    }

    private void OnDisable()
    {
        DialogEventChannel?.RemoveListener<EndDialogEvent>(OnEndDialogEvent);
        if (PlayableDirector != null) PlayableDirector.stopped -= HandleDirectorStopped;
        CancelWait();
    }

    private void HandleDirectorStopped(PlayableDirector director) => CancelWait();

    private void CancelWait()
    {
        if (_playDialogCoroutine != null) StopCoroutine(_playDialogCoroutine);
        _playDialogCoroutine = null;
        _waitingDialogEnd = false;
        ReleaseTimePause();
        ResumeTimeline();
    }

    public void SetDialogData(DialogDataSO dialogData) => DialogData = dialogData;

    public void PlayDialogSignal()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || _waitingDialogEnd ||
            _playDialogCoroutine != null || DialogManager.Talking || BubbleDialogManager.Talking)
            return;

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
        if (_pausedRoots.Count > 0 || PlayableDirector == null || !PlayableDirector.playableGraph.IsValid())
        {
            return;
        }

        PlayableGraph graph = PlayableDirector.playableGraph;
        for (int i = 0; i < graph.GetRootPlayableCount(); i++)
        {
            Playable root = graph.GetRootPlayable(i);
            _pausedRoots.Add(root);
            _previousSpeeds.Add(root.GetSpeed());
            root.SetSpeed(0);
        }
    }

    public void ResumeTimeline()
    {
        for (int i = 0; i < _pausedRoots.Count; i++)
            if (_pausedRoots[i].IsValid()) _pausedRoots[i].SetSpeed(_previousSpeeds[i]);
        _pausedRoots.Clear();
        _previousSpeeds.Clear();
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
        if (_timePause == null)
        {
            return;
        }

        bool resumeTimeline = _waitingDialogEnd;
        _waitingDialogEnd = false;
        ReleaseTimePause();
        if (resumeTimeline)
            ResumeTimeline();
    }

    private void ReleaseTimePause()
    {
        _timePause?.Dispose();
        _timePause = null;
    }

    private IEnumerator RaiseDialogNextFrame(DialogDataSO dialogData)
    {
        yield return null;

        _playDialogCoroutine = null;
        var request = new StartDialogEvent().InitData(dialogData);
        DialogEventChannel.RaiseEvent(request);
        if (!request.Accepted)
        {
            Debug.LogWarning("Timeline dialogue was not accepted. Check the active DialogManager and event channel.", this);
            CancelWait();
        }
        else
        {
            _timePause = TimeManager.Instance.Pause();
        }
    }
}
