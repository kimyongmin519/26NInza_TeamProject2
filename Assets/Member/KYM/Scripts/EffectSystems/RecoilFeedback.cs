using System;
using DG.Tweening;
using GGMLib.CoreLibrary;
using UnityEngine;

namespace Member.KYM.Scripts.EffectSystems
{
    public class RecoilFeedback : AbstractFeedback
    {
        [SerializeField] private float recoilAmount;
        [SerializeField] private float recoilDuration;
        [SerializeField] private float returnDuration;
        [SerializeField] private Ease recoilEase;
        [SerializeField] private Transform recoilTarget;

        private float _originX;
        private Sequence _sequence;

        private void Awake()
        {
            _originX = recoilTarget.localPosition.x;
        }

        public override void PlayFeedback()
        {
            _sequence = DOTween.Sequence();
            
            _sequence.Append(recoilTarget.DOLocalMoveX(recoilAmount, recoilDuration).SetEase(recoilEase));
            _sequence.AppendCallback(() => recoilTarget.DOLocalMoveX(_originX, returnDuration).SetEase(recoilEase));
        }
    }
}