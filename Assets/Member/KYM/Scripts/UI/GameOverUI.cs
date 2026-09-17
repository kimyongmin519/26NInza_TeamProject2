using System;
using DG.Tweening;
using KimLIb.AnimatorSystems;
using Member.KYM.Scripts.CoreSystems;
using UnityEngine;

namespace Member.KYM.Scripts.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private RectTransform panelRectTrm;
        [SerializeField] private Transform robotArmVisual;
        [SerializeField] private AnimParamSO robotArmAnimParam;
        
        [Header("트윈 애니메이션 값")]
        [SerializeField] private Vector2 panelShowPos;
        [SerializeField] private Vector3 panelRotation;
        
        private Animator _robotArmAnimator;
        private AnimatorTrigger _robotArmTrigger;

        private void Awake()
        {
            _robotArmAnimator = robotArmVisual.GetComponent<Animator>();
            _robotArmTrigger = robotArmVisual.GetComponent<AnimatorTrigger>();
        }


        [ContextMenu("Play")]
        public void ShowGameOverUI()
        {
            _robotArmAnimator.Play(robotArmAnimParam.ParamHash);

            _robotArmTrigger.OnSpecialEvent += HandlePanelShow;
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