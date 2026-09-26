using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

namespace Member.KYM.Scripts.CoreSystems.PostProcessSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Volume))]
    public class SunlightBrightness : MonoBehaviour
    {
        [Header("태양 등장 밝기 전환")]
        [SerializeField, Min(0f)] private float duration = 2f;
        [SerializeField] private Ease ease = Ease.InOutSine;

        private Volume _volume;
        private Tween _tween;

        private void Awake()
        {
            _volume = GetComponent<Volume>();
            _volume.weight = 0f;
        }

        private void OnDisable()
        {
            _tween?.Kill();
            _tween = null;

            if (_volume != null)
                _volume.weight = 0f;
        }

        public void Brighten()
        {
            if (_volume == null)
                return;

            _tween?.Kill();

            if (duration <= 0f)
            {
                _volume.weight = 1f;
                return;
            }

            _tween = DOTween.To(
                    () => _volume.weight,
                    value => _volume.weight = value,
                    1f,
                    duration)
                .SetEase(ease)
                .SetUpdate(true)
                .OnComplete(() => _tween = null);
        }

        public void ResetBrightness()
        {
            _tween?.Kill();
            _tween = null;

            if (_volume != null)
                _volume.weight = 0f;
        }
    }
}
