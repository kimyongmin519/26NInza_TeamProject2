//using DG.Tweening;
//using KimLIb.EventSystem;
//using UnityEngine;




//public class Highlight : MonoBehaviour
//{
//    [SerializeField] private EventChannelSO UiEventChannel;
//    [SerializeField] private RectTransform TopBar;
//    [SerializeField] private RectTransform BottomBar;
//    [SerializeField] private Vector2 TopHidePosition = new Vector2(0f, 900f);
//    [SerializeField] private Vector2 BottomHidePosition = new Vector2(0f, -900f);
//    [SerializeField] private Vector2 TopShowPosition = new Vector2(0f, -160f);
//    [SerializeField] private Vector2 BottomShowPosition = new Vector2(0f, 160f);


//    [SerializeField] private float MoveDuration = 0.35f;
//    [SerializeField] private Ease Ease = Ease.OutCubic;

//    private Sequence _sequence;


//    private void Awake()
//    {
//        UiEventChannel.AddListener<HighlightEvent>(HandleHighlightEvent);
//    }

//    private void OnDestroy()
//    {
//        UiEventChannel.RemoveListener<HighlightEvent>(HandleHighlightEvent);
//        _sequence?.Kill();

//    }

//    public void ShowHighlight() => SetHighlight(true);
//    public void HideHighlight() => SetHighlight(false);

//    public void SetPosition(Vector2 topHidePosition, Vector2 bottomHidePosition, Vector2 topShowPosition, Vector2 bottomShowPosition)
//    {
//        TopHidePosition = topHidePosition;
//        BottomHidePosition = bottomHidePosition;
//        TopShowPosition = topShowPosition;
//        BottomShowPosition = bottomShowPosition;
//    }

//    public void SetMoveDuration(float moveDuration) => MoveDuration = moveDuration;

//    private void HandleHighlightEvent(HighlightEvent evt)
//    {
//        SetHighlight(evt.IsShow);
//    }

//    public void SetHighlight(bool isShow)
//    {
//        Vector2 topPosition = isShow ? TopShowPosition : TopHidePosition;
//        Vector2 bottomPosition = isShow ? BottomShowPosition : BottomHidePosition;


//        _sequence?.Kill();
//        TopBar.DOKill();
//        BottomBar.DOKill();

//        _sequence = DOTween.Sequence();
//        _sequence.Append(TopBar.DOAnchorPos(topPosition, MoveDuration).SetEase(Ease));
//        _sequence.Join(BottomBar.DOAnchorPos(bottomPosition, MoveDuration).SetEase(Ease));

//        TopBar.anchoredPosition = topPosition;
//        BottomBar.anchoredPosition = bottomPosition;

//    }

//    public void SetHighlightInstant(bool isShow)
//    {
//        Vector2 topPosition = isShow ? TopShowPosition : TopHidePosition;
//        Vector2 bottomPosition = isShow ? BottomShowPosition : BottomHidePosition;


//        _sequence?.Kill();
//        TopBar.DOKill();
//        BottomBar.DOKill();


//        TopBar.anchoredPosition = topPosition;
//        BottomBar.anchoredPosition = bottomPosition;
//    }
//}
