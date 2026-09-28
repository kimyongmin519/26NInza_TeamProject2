using System.Collections;
using System.Collections.Generic;
using KimLIb.EventSystem;
using UnityEngine;
using UnityEngine.Playables;

[DisallowMultipleComponent]
public sealed class TimelineBubbleDialogSignal : MonoBehaviour
{
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private EventChannelSO bubbleDialogEventChannel;
    [SerializeField] private EventChannelSO cameraEventChannel;
    [SerializeField] private DialogDataSO dialogData;
    [SerializeField] private Transform target;
    [SerializeField] private bool pauseTimelineUntilDialogEnd = true;

    private readonly List<Playable> _pausedRoots = new List<Playable>();
    private readonly List<double> _previousSpeeds = new List<double>();
    private Coroutine _pending;
    private StartBubbleDialogEvent _request;
    private int _generation;
    private static readonly HashSet<PlayableDirector> WaitingDirectors = new HashSet<PlayableDirector>();
    private bool _ownsDirector;

    private void Reset() => playableDirector = GetComponent<PlayableDirector>();
    private void Awake()
    {
        if (playableDirector == null)
            playableDirector = GetComponent<PlayableDirector>();
    }
    private void OnEnable()
    {
        if (playableDirector != null) playableDirector.stopped += OnDirectorStopped;
    }
    private void OnDisable()
    {
        if (playableDirector != null) playableDirector.stopped -= OnDirectorStopped;
        CancelDialog();
    }

    public void SetDialogData(DialogDataSO data) => dialogData = data;
    public void SetTarget(Transform npc) => target = npc;
    public void PlayBubbleDialogSignal(DialogDataSO data)
    {
        dialogData = data;
        PlayBubbleDialogSignal();
    }

    public void PlayBubbleDialogSignal() => BeginDialog(dialogData, target);

    public void PlayBubbleDialogForTarget(Transform npc) => BeginDialog(dialogData, npc);

    private void BeginDialog(DialogDataSO data, Transform npc)
    {
        // Timeline preview must not lock gameplay input or start a live conversation.
        if (!Application.isPlaying || !isActiveAndEnabled || _request != null || _pending != null)
            return;
        if (bubbleDialogEventChannel == null || data == null || npc == null)
        {
            Debug.LogWarning("Timeline Bubble needs a channel, Dialog Data and NPC Target.", this);
            return;
        }
        if (BubbleDialogManager.Talking || (playableDirector != null && !WaitingDirectors.Add(playableDirector)))
        {
            Debug.LogWarning("A Bubble conversation is already running. Place dialogue signals at separate times.", this);
            return;
        }
        _ownsDirector = playableDirector != null;
        int generation = ++_generation;
        if (pauseTimelineUntilDialogEnd) PauseRoots();
        _pending = StartCoroutine(StartNextFrame(generation, data, npc));
    }

    private IEnumerator StartNextFrame(int generation, DialogDataSO data, Transform npc)
    {
        yield return null;
        _pending = null;
        if (generation != _generation) yield break;
        if (npc == null) { Complete(generation); yield break; }
        var request = new StartBubbleDialogEvent().InitData(data, npc, () => Complete(generation));
        _request = request;
        bubbleDialogEventChannel.RaiseEvent(request);
        if (!request.Accepted)
        {
            Debug.LogWarning("No active BubbleDialogManager accepted the dialogue. Check its event channel.", this);
            bubbleDialogEventChannel.RaiseEvent(new EndBubbleDialogEvent());
            Complete(generation);
        }
        else if (_request == request)
            cameraEventChannel?.RaiseEvent(new FocusCameraTargetEvent().Init(npc));
    }

    private void Complete(int generation)
    {
        if (generation != _generation) return;
        _request = null;
        RestoreRoots();
        ReleaseDirector();
    }

    public void CancelDialog()
    {
        _generation++;
        if (_pending != null) StopCoroutine(_pending);
        _pending = null;
        StartBubbleDialogEvent request = _request;
        _request = null;
        if (request != null)
            bubbleDialogEventChannel?.RaiseEvent(new CancelBubbleDialogEvent { Request = request });
        RestoreRoots();
        ReleaseDirector();
    }

    private void ReleaseDirector()
    {
        if (_ownsDirector) WaitingDirectors.Remove(playableDirector);
        _ownsDirector = false;
    }

    private void OnDirectorStopped(PlayableDirector director) => CancelDialog();

    private void PauseRoots()
    {
        if (playableDirector == null || !playableDirector.playableGraph.IsValid()) return;
        PlayableGraph graph = playableDirector.playableGraph;
        for (int i = 0; i < graph.GetRootPlayableCount(); i++)
        {
            Playable root = graph.GetRootPlayable(i);
            _pausedRoots.Add(root);
            _previousSpeeds.Add(root.GetSpeed());
            root.SetSpeed(0);
        }
    }

    private void RestoreRoots()
    {
        for (int i = 0; i < _pausedRoots.Count; i++)
            if (_pausedRoots[i].IsValid()) _pausedRoots[i].SetSpeed(_previousSpeeds[i]);
        _pausedRoots.Clear();
        _previousSpeeds.Clear();
    }
}
