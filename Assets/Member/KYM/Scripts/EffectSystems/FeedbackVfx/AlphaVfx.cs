using DG.Tweening;
using UnityEngine;

namespace Member.KYM.Scripts.EffectSystems.FeedbackVfx
{
    public class AlphaVfx : MonoBehaviour, IFeedbackVfx
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [Range(0f, 1f)]
        [SerializeField] private float targetAlpha;
        [SerializeField] private float fadingDuration;
        [SerializeField] private Ease fadingEase;

        private float _baseAlpha;
        private Tween _alphaTween;

        private void Awake()
        {
            _baseAlpha = targetRenderer.color.a;
        }

        public void PlayFeedbackVfx()
        {
            _alphaTween?.Kill();
            Color color = targetRenderer.color;
            color.a = _baseAlpha;
            targetRenderer.color = color;
            _alphaTween = targetRenderer.DOFade(targetAlpha, fadingDuration)
                .SetEase(fadingEase).SetUpdate(true);
        }

        public void StopFeedbackVfx()
        {
            _alphaTween?.Kill();
            _alphaTween = null;
            Color color = targetRenderer.color;
            color.a = _baseAlpha;
            targetRenderer.color = color;
        }
    }
}