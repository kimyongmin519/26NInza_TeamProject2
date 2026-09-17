using KimLIb.EventSystem;
using System.Collections;
using UnityEngine;

public class BubbleDialogUI : MonoBehaviour
{
    [SerializeField] private EventChannelSO BubbleDialogEventChannel;
    [SerializeField] private BubbleDialogUIContainer BubbleDialogUIContainer;
    [SerializeField] private GameObject BubbleUIObject;
    [SerializeField] private RectTransform BubbleUIRect;
    [SerializeField] private Canvas TargetCanvas;
    [SerializeField] private Camera WorldCamera;
    [SerializeField] private Vector3 WorldOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private Vector2 ScreenOffset;
    [SerializeField] private bool ClampToCanvas = true;
    [SerializeField] private Vector2 CanvasPadding = new Vector2(24f, 24f);
    [SerializeField] private float ShowDuration = 0.15f;
    [SerializeField] private float HideDuration = 0.1f;
    [SerializeField] private Vector3 HideScale = new Vector3(0.85f, 0.85f, 1f);

    private RectTransform _canvasRect;
    private CanvasGroup _canvasGroup;
    private Transform _target;
    private Coroutine _bubbleCoroutine;

    private void Awake()
    {
        ResolveReferences();

        BubbleDialogEventChannel?.AddListener<StartBubbleDialogEvent>(OnStartBubbleDialogEvent);
        BubbleDialogEventChannel?.AddListener<SetBubbleDialogLineEvent>(OnSetBubbleDialogLineEvent);
        BubbleDialogEventChannel?.AddListener<SkipBubbleDialogLineEvent>(OnSkipBubbleDialogLineEvent);
        BubbleDialogEventChannel?.AddListener<EndBubbleDialogEvent>(OnEndBubbleDialogEvent);

        SetBubbleInstant(false);
    }

    private void OnDestroy()
    {
        BubbleDialogEventChannel?.RemoveListener<StartBubbleDialogEvent>(OnStartBubbleDialogEvent);
        BubbleDialogEventChannel?.RemoveListener<SetBubbleDialogLineEvent>(OnSetBubbleDialogLineEvent);
        BubbleDialogEventChannel?.RemoveListener<SkipBubbleDialogLineEvent>(OnSkipBubbleDialogLineEvent);
        BubbleDialogEventChannel?.RemoveListener<EndBubbleDialogEvent>(OnEndBubbleDialogEvent);

        StopBubbleCoroutine();
    }

    private void LateUpdate()
    {
        if (BubbleUIObject != null && BubbleUIObject.activeSelf)
        {
            UpdateTargetPosition();
        }
    }

    public void SetTarget(Transform target)
    {
        _target = target;
        UpdateTargetPosition();
    }

    private void OnStartBubbleDialogEvent(StartBubbleDialogEvent evt)
    {
        SetTarget(evt.Target);
        ShowBubbleUI();
    }

    private void OnSetBubbleDialogLineEvent(SetBubbleDialogLineEvent evt)
    {
        SetTarget(evt.Target);
        if (BubbleDialogUIContainer == null)
        {
            return;
        }

        BubbleDialogUIContainer.Set(evt.SpeakerName, evt.Description, evt.TalkEndAction);
    }

    private void OnSkipBubbleDialogLineEvent(SkipBubbleDialogLineEvent evt)
    {
        if (BubbleDialogUIContainer == null)
        {
            return;
        }

        BubbleDialogUIContainer.SkipText();
    }

    private void OnEndBubbleDialogEvent(EndBubbleDialogEvent evt)
    {
        HideBubbleUI();
    }

    private void ShowBubbleUI()
    {
        StopBubbleCoroutine();

        if (BubbleUIObject == null)
        {
            return;
        }

        BubbleUIObject.SetActive(true);
        UpdateTargetPosition();
        _bubbleCoroutine = StartCoroutine(ScaleBubbleCoroutine(Vector3.one, 1f, ShowDuration, null));
    }

    private void HideBubbleUI()
    {
        StopBubbleCoroutine();

        if (BubbleUIObject == null)
        {
            return;
        }

        _bubbleCoroutine = StartCoroutine(ScaleBubbleCoroutine(HideScale, 0f, HideDuration, () =>
        {
            BubbleDialogUIContainer?.Clear();
            BubbleUIObject.SetActive(false);
            _target = null;
        }));
    }

    private void SetBubbleInstant(bool isShow)
    {
        if (BubbleUIObject == null || BubbleUIRect == null)
        {
            return;
        }

        BubbleUIObject.SetActive(isShow);
        BubbleUIRect.localScale = isShow ? Vector3.one : HideScale;

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = isShow ? 1f : 0f;
        }

        if (isShow == false)
        {
            BubbleDialogUIContainer?.Clear();
        }
    }

    private IEnumerator ScaleBubbleCoroutine(Vector3 targetScale, float targetAlpha, float duration, System.Action onComplete)
    {
        Vector3 startScale = BubbleUIRect.localScale;
        float startAlpha = _canvasGroup != null ? _canvasGroup.alpha : targetAlpha;
        float currentTime = 0f;

        if (duration <= 0f)
        {
            BubbleUIRect.localScale = targetScale;
            SetAlpha(targetAlpha);
            onComplete?.Invoke();
            yield break;
        }

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float percent = Mathf.Clamp01(currentTime / duration);
            percent = Mathf.Sin(percent * Mathf.PI * 0.5f);

            BubbleUIRect.localScale = Vector3.Lerp(startScale, targetScale, percent);
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, percent));
            yield return null;
        }

        BubbleUIRect.localScale = targetScale;
        SetAlpha(targetAlpha);
        _bubbleCoroutine = null;
        onComplete?.Invoke();
    }

    private void UpdateTargetPosition()
    {
        if (_target == null || TargetCanvas == null || BubbleUIRect == null || _canvasRect == null)
        {
            return;
        }

        Camera worldCamera = WorldCamera != null ? WorldCamera : Camera.main;
        Vector3 screenPosition = RectTransformUtility.WorldToScreenPoint(worldCamera, _target.position + WorldOffset);
        Camera canvasCamera = TargetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : TargetCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPosition, canvasCamera, out Vector2 localPoint) == false)
        {
            return;
        }

        localPoint += ScreenOffset;

        if (ClampToCanvas)
        {
            localPoint = ClampPositionToCanvas(localPoint);
        }

        BubbleUIRect.anchoredPosition = localPoint;
    }

    private Vector2 ClampPositionToCanvas(Vector2 localPoint)
    {
        Rect canvasRect = _canvasRect.rect;

        float minX = canvasRect.xMin + CanvasPadding.x;
        float maxX = canvasRect.xMax - CanvasPadding.x;
        float minY = canvasRect.yMin + CanvasPadding.y;
        float maxY = canvasRect.yMax - CanvasPadding.y;

        localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);
        localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);

        return localPoint;
    }

    private void ResolveReferences()
    {
        if (BubbleUIObject == null)
        {
            BubbleUIObject = gameObject;
        }

        if (BubbleUIRect == null)
        {
            BubbleUIRect = BubbleUIObject.GetComponent<RectTransform>();
        }

        if (BubbleDialogUIContainer == null)
        {
            BubbleDialogUIContainer = GetComponentInChildren<BubbleDialogUIContainer>(true);
        }

        if (TargetCanvas == null)
        {
            TargetCanvas = GetComponentInParent<Canvas>();
        }

        if (TargetCanvas != null)
        {
            _canvasRect = TargetCanvas.GetComponent<RectTransform>();
        }

        _canvasGroup = BubbleUIObject.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = BubbleUIObject.AddComponent<CanvasGroup>();
        }
    }

    private void SetAlpha(float alpha)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = alpha;
        }
    }

    private void StopBubbleCoroutine()
    {
        if (_bubbleCoroutine == null)
        {
            return;
        }

        StopCoroutine(_bubbleCoroutine);
        _bubbleCoroutine = null;
    }
}
