using DG.Tweening;
using KimLIb.AnimatorSystems;
using Member.KYM.Scripts.CoreSystems;
using Member.KYM.Scripts.Players;
using UnityEngine;

namespace Member.KYM.Scripts.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private GameObject playerObject;
        [SerializeField] private GameObject gameOverRoot;
        [SerializeField] private RectTransform panelRectTrm;
        [SerializeField] private Transform robotArmVisual;
        [SerializeField] private AnimParamSO robotArmAnimParam;
        
        [Header("트윈 애니메이션 값")]
        [SerializeField] private Vector2 panelShowPos;
        [SerializeField] private Vector3 panelRotation;
        
        private Animator _robotArmAnimator;
        private AnimatorTrigger _robotArmTrigger;
        private PlayerController _player;
        private bool _isShown;

        private void Awake()
        {
            _player = playerObject.GetComponent<PlayerController>();
            _robotArmAnimator = robotArmVisual.GetComponent<Animator>();
            _robotArmTrigger = robotArmVisual.GetComponent<AnimatorTrigger>();
        }

        private void OnEnable()
        {
            _player.OnDeath.AddListener(ShowGameOverUI);
        }

        private void OnDisable()
        {
            _player.OnDeath.RemoveListener(ShowGameOverUI);
            _robotArmTrigger.OnSpecialEvent -= HandlePanelShow;
        }

        [ContextMenu("Play")]
        public void ShowGameOverUI()
        {
            if (_isShown)
                return;

            _isShown = true;
            gameOverRoot.SetActive(true);
            _robotArmTrigger.OnSpecialEvent += HandlePanelShow;
            _robotArmAnimator.Play(robotArmAnimParam.ParamHash);
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
