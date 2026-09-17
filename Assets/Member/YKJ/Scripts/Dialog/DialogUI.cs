using KimLIb.EventSystem;
using System.Collections;
using UnityEngine;


using DG.Tweening;


public class DialogUI : MonoBehaviour
{
    [SerializeField] private EventChannelSO DialogEventChannel;
    [SerializeField] private DialogUIContainer DialogUIContainer;
    [SerializeField] private GameObject DialogUIObject;
    [SerializeField] private GameObject BackGroundObject;
    [SerializeField] private RectTransform DialogUIRect;
    [SerializeField] private Vector2 ShowPosition = Vector2.zero;
    [SerializeField] private Vector2 ShowStartPosition = new Vector2(-1200f, 0f);
    [SerializeField] private Vector2 HideEndPosition = new Vector2(1200f, 0f);
    [SerializeField] private float ShowDuration = 0.35f;
    [SerializeField] private float HideDuration = 0.25f;


    [SerializeField] private Ease ShowEase = Ease.OutBack;
    [SerializeField] private Ease HideEase = Ease.InBack;
    private Tween _dialogTween;

    private Coroutine _dialogCoroutine;


    private void Awake()
    {
        DialogEventChannel.AddListener<SetDialogLineEvent>(OnSetDialogLineEvent);
        DialogEventChannel.AddListener<SkipDialogLineEvent>(OnSkipDialogLineEvent);
        DialogEventChannel.AddListener<StartDialogEvent>(OnStartDialogEvent);
        DialogEventChannel.AddListener<EndDialogEvent>(OnEndDialogEvent);
        if (DialogUIContainer == null)
        {
            DialogUIContainer = GetComponentInChildren<DialogUIContainer>(true);
        }

        if (DialogUIRect == null)
        {
            DialogUIRect = DialogUIObject.GetComponent<RectTransform>();
        }

        SetDialogUIInstant(false);
    }

    private void OnDestroy()
    {
        DialogEventChannel.RemoveListener<SetDialogLineEvent>(OnSetDialogLineEvent);
        DialogEventChannel.RemoveListener<SkipDialogLineEvent>(OnSkipDialogLineEvent);
        DialogEventChannel.RemoveListener<StartDialogEvent>(OnStartDialogEvent);
        DialogEventChannel.RemoveListener<EndDialogEvent>(OnEndDialogEvent);

        _dialogTween?.Kill();

        StopDialogCoroutine();

    }

    private void OnSetDialogLineEvent(SetDialogLineEvent evt)
    {
        DialogUIContainer.Set(evt.Id, evt.SpeakerName, evt.Description, evt.SpeakeIcon, evt.TalkEndAction);
    }

    private void OnSkipDialogLineEvent(SkipDialogLineEvent evt)
    {
        DialogUIContainer.SkipText();
    }

    private void OnStartDialogEvent(StartDialogEvent evt)
    {
        ShowDialogUI();
    }

    private void OnEndDialogEvent(EndDialogEvent evt)
    {
        HideDialogUI();
    }

    private void ShowDialogUI()
    {

        _dialogTween?.Kill();
        DialogUIRect.DOKill();

        StopDialogCoroutine();


        DialogUIObject.SetActive(true);
        SetBackGroundActive(true);
        DialogUIRect.anchoredPosition = ShowStartPosition;


        _dialogTween = DialogUIRect
            .DOAnchorPos(ShowPosition, ShowDuration)
            .SetEase(ShowEase);

        _dialogCoroutine = StartCoroutine(MoveDialogCoroutine(ShowPosition, ShowDuration, null));

    }

    private void HideDialogUI()
    {

        _dialogTween?.Kill();
        DialogUIRect.DOKill();

        _dialogTween = DialogUIRect
            .DOAnchorPos(HideEndPosition, HideDuration)
            .SetEase(HideEase)
            .OnComplete(() =>
            {
                DialogUIContainer.Clear();
                DialogUIObject.SetActive(false);
                SetBackGroundActive(false);
            });

        StopDialogCoroutine();
        _dialogCoroutine = StartCoroutine(MoveDialogCoroutine(HideEndPosition, HideDuration, () =>
        {
            DialogUIContainer.Clear();
            DialogUIObject.SetActive(false);
            SetBackGroundActive(false);
        }));

    }

    private void SetDialogUIInstant(bool isShow)
    {
        DialogUIObject.SetActive(isShow);
        SetBackGroundActive(isShow);
        DialogUIRect.anchoredPosition = isShow ? ShowPosition : ShowStartPosition;

        if (isShow == false)
        {
            DialogUIContainer.Clear();
        }
    }

    private void SetBackGroundActive(bool isActive)
    {
        if (BackGroundObject == null)
        {
            return;
        }

        BackGroundObject.SetActive(isActive);
    }


    private IEnumerator MoveDialogCoroutine(Vector2 targetPosition, float duration, System.Action onComplete)
    {
        if (duration <= 0f)
        {
            DialogUIRect.anchoredPosition = targetPosition;
            onComplete?.Invoke();
            yield break;
        }

        Vector2 startPosition = DialogUIRect.anchoredPosition;
        float currentTime = 0f;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float percent = Mathf.Clamp01(currentTime / duration);
            percent = Mathf.Sin(percent * Mathf.PI * 0.5f);
            DialogUIRect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, percent);
            yield return null;
        }

        DialogUIRect.anchoredPosition = targetPosition;
        _dialogCoroutine = null;
        onComplete?.Invoke();
    }

    private void StopDialogCoroutine()
    {
        if (_dialogCoroutine == null)
        {
            return;
        }

        StopCoroutine(_dialogCoroutine);
        _dialogCoroutine = null;
    }

}
