using DG.Tweening;
using Member.KYM.Scripts.CoreSystems.Managers;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Member.KYM.Scripts.CoreSystems.PostProcessSystem
{
    public class ParryImpactVingnette : MonoBehaviour, IPostProcessEffect
    {
        [Header("볼륨")]
        [SerializeField] private Volume volume;

        [Header("캐치 비네트")]
        [SerializeField, Range(0f, 1f)] private float intensity = 0.5f;
        [SerializeField, Min(0f)] private float duration = 0.1f;
        [SerializeField, Min(0f)] private float holdDuration = 0.1f;
        [SerializeField, Min(0f)] private float returnDuration = 0.25f;
        
        [Header("캐치 타임")]
        [SerializeField, Min(0f)] private float stopDuration = 0.15f;

        public PostProcessType Type => PostProcessType.ParryImpact;

        private Vignette _vignette;
        private Sequence _sequence;

        private void Awake()
        {
            if (TryInitialize())
                SetIntensity(0f);

            if (volume != null)
                volume.weight = 0f;
        }

        private void OnDisable()
        {
            KillSequence();

            if (_vignette != null)
                SetIntensity(0f);

            if (volume != null)
                volume.weight = 0f;
        }

        public void Handle(PostProcessRequest request)
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (!TryInitialize())
                return;

            switch (request.Command)
            {
                case PostProcessCommand.Play:
                    Play();
                    break;
                case PostProcessCommand.Stop:
                    ReturnToZero();
                    break;
            }
        }

        private void Play()
        {
            KillSequence();
            volume.weight = 1f;

            TimeManager.Instance.StopTimer(stopDuration);
            _sequence = DOTween.Sequence().SetUpdate(true);

            if (duration > 0f)
            {
                _sequence.Append(
                    DOTween.To(
                            () => _vignette.intensity.value,
                            SetIntensity,
                            intensity,
                            duration)
                        .SetEase(Ease.OutQuad));
            }
            else
            {
                SetIntensity(intensity);
            }

            if (holdDuration > 0f)
                _sequence.AppendInterval(holdDuration);

            AppendReturnTween(_sequence);
            _sequence.OnComplete(FinishEffect);
        }

        private void ReturnToZero()
        {
            KillSequence();

            if (returnDuration <= 0f)
            {
                FinishEffect();
                return;
            }

            _sequence = DOTween.Sequence().SetUpdate(true);
            AppendReturnTween(_sequence);
            _sequence.OnComplete(FinishEffect);
        }

        private void FinishEffect()
        {
            SetIntensity(0f);
            volume.weight = 0f;
            _sequence = null;
        }

        private void AppendReturnTween(Sequence sequence)
        {
            if (returnDuration <= 0f)
            {
                sequence.AppendCallback(() => SetIntensity(0f));
                return;
            }

            sequence.Append(
                DOTween.To(
                        () => _vignette.intensity.value,
                        SetIntensity,
                        0f,
                        returnDuration)
                    .SetEase(Ease.InQuad));
        }

        private bool TryInitialize()
        {
            if (_vignette != null)
                return true;

            if (volume == null)
                volume = GetComponent<Volume>();

            if (volume == null ||
                volume.profile == null ||
                !volume.profile.TryGet(out _vignette))
            {
                Debug.LogWarning("HurtVignette에 Vignette가 포함된 Volume이 필요합니다.", this);
                return false;
            }

            _vignette.intensity.overrideState = true;
            return true;
        }

        private void SetIntensity(float value)
        {
            _vignette.intensity.value = Mathf.Clamp01(value);
        }

        private void KillSequence()
        {
            _sequence?.Kill();
            _sequence = null;
        }

        private void OnValidate()
        {
            intensity = Mathf.Clamp01(intensity);
            duration = Mathf.Max(0f, duration);
            holdDuration = Mathf.Max(0f, holdDuration);
            returnDuration = Mathf.Max(0f, returnDuration);
        }
    }
}
