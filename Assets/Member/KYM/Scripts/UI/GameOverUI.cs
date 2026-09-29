using DG.Tweening;
using KimLIb.AnimatorSystems;
using Member.KYM.Scripts.CoreSystems;
using KimLIb.EventSystem;
using Member.KYM.Scripts.CoreSystems.Events;
using UnityEngine;

namespace Member.KYM.Scripts.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private EventChannelSO uiChannel;
        [SerializeField] private GameObject gameOverRoot;
        [SerializeField] private RectTransform panelRectTrm;
        [SerializeField] private Transform robotArmVisual;
        [SerializeField] private AnimParamSO robotArmAnimParam;
        
        [Header("트윈 애니메이션 값")]
        [SerializeField] private Vector2 panelShowPos;
        [SerializeField] private Vector3 panelRotation;
        
        private Animator _robotArmAnimator;
        private AnimatorTrigger _robotArmTrigger;
        private Canvas _canvas;
        private bool _isShown;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _robotArmAnimator = robotArmVisual.GetComponent<Animator>();
            _robotArmTrigger = robotArmVisual.GetComponent<AnimatorTrigger>();
        }

        private void OnEnable()
        {
            BindMainCamera();
            if (uiChannel == null) return;
            uiChannel.AddListener<PlayerUIStateEvent>(HandleState);
            uiChannel.RaiseEvent(new PlayerUIStateRequest());
        }

        private void OnDisable()
        {
            if (uiChannel != null)
                uiChannel.RemoveListener<PlayerUIStateEvent>(HandleState);
            _robotArmTrigger.OnSpecialEvent -= HandlePanelShow;
        }

        [ContextMenu("Play")]
        public void ShowGameOverUI()
        {
            if (_isShown)
                return;

            BindMainCamera();
            _isShown = true;
            gameOverRoot.SetActive(true);
            _robotArmTrigger.OnSpecialEvent += HandlePanelShow;
            _robotArmAnimator.Play(robotArmAnimParam.ParamHash);
        }

        private void HandleState(PlayerUIStateEvent evt)
        {
            if (evt.IsDead) ShowGameOverUI();
        }

        private void BindMainCamera()
        {
            if (_canvas == null || _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return;

            Camera mainCamera = Camera.main;
            if (mainCamera != null && _canvas.worldCamera != mainCamera)
                _canvas.worldCamera = mainCamera;
        }

        private void HandlePanelShow()
        {
            _robotArmTrigger.OnSpecialEvent -= HandlePanelShow;
            
            Sequence sequence = DOTween.Sequence();
            sequence.Append(panelRectTrm.DOAnchorPos(panelShowPos, 0.75f)).SetEase(Ease.Linear);
            sequence.Append(panelRectTrm.DOLocalRotate(panelRotation, 0.5f).SetEase(Ease.OutBack));
        }
    }
}
