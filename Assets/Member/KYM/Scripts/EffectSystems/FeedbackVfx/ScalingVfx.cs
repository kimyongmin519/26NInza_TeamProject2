using DG.Tweening;
using UnityEngine;

namespace Member.KYM.Scripts.EffectSystems.FeedbackVfx
{
    public class ScalingVfx : MonoBehaviour, IFeedbackVfx
    {
        [SerializeField] private Vector3 targetScale;
        [SerializeField] private float scalingDuration;
        [SerializeField] private Ease scalingEase;

        private Vector3 _baseScale;
        private Tween _scalingTween;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        public void PlayFeedbackVfx()
        {
            _scalingTween?.Kill();
            transform.localScale = _baseScale;
            _scalingTween = transform.DOScale(targetScale, scalingDuration)
                .SetEase(scalingEase).SetUpdate(true);
        }

        public void StopFeedbackVfx()
        {
            _scalingTween?.Kill();
            _scalingTween = null;
            transform.localScale = _baseScale;
        }
    }
}