using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Member.KYM.Scripts.CoreSystems.TutorialSystem
{
    [DisallowMultipleComponent]
    public sealed class TutorialDoor : MonoBehaviour
    {
        [Header("움직일 문")]
        [SerializeField] private Transform doorBody;
        [SerializeField] private Vector3 openLocalOffset = new(0f, 3f, 0f);

        [Header("열리기 전 떨림")]
        [SerializeField, Min(0f)] private float shakeDuration = 0.4f;
        [SerializeField] private Vector3 shakeStrength = new(0.08f, 0.04f, 0f);
        [SerializeField, Min(1)] private int shakeVibrato = 18;

        [Header("문 이동")]
        [SerializeField, Min(0f)] private float openDuration = 0.75f;
        [SerializeField] private Ease openEase = Ease.InOutCubic;
        [SerializeField, Min(0f)] private float closeDuration = 0.5f;
        [SerializeField] private Ease closeEase = Ease.InOutSine;

        [Header("완료 이벤트")]
        [SerializeField] private UnityEvent onOpened;

        public bool IsOpen { get; private set; }

        private Vector3 _closedLocalPosition;
        private Sequence _sequence;

        private void Awake()
        {
            if (doorBody == null)
                doorBody = transform;

            _closedLocalPosition = doorBody.localPosition;
        }

        public void Open()
        {
            if (IsOpen || _sequence != null && _sequence.IsActive())
                return;

            doorBody.localPosition = _closedLocalPosition;
            Vector3 openPosition = _closedLocalPosition + openLocalOffset;

            _sequence = DOTween.Sequence();
            if (shakeDuration > 0f && shakeStrength.sqrMagnitude > 0f)
            {
                _sequence.Append(doorBody.DOShakePosition(
                    shakeDuration, shakeStrength, shakeVibrato));
                _sequence.AppendCallback(() => doorBody.localPosition = _closedLocalPosition);
            }

            _sequence.Append(doorBody.DOLocalMove(openPosition, openDuration).SetEase(openEase));
            _sequence.OnComplete(() =>
            {
                doorBody.localPosition = openPosition;
                IsOpen = true;
                _sequence = null;
                onOpened?.Invoke();
            });
        }

        public void Close()
        {
            if (!IsOpen)
                return;

            StopTween();
            IsOpen = false;
            _sequence = DOTween.Sequence();
            _sequence.Append(doorBody.DOLocalMove(_closedLocalPosition, closeDuration).SetEase(closeEase));
            _sequence.OnComplete(() =>
            {
                doorBody.localPosition = _closedLocalPosition;
                _sequence = null;
            });
        }

        private void OnDisable()
        {
            StopTween();
            if (!IsOpen && doorBody != null)
                doorBody.localPosition = _closedLocalPosition;
        }

        private void StopTween()
        {
            _sequence?.Kill();
            _sequence = null;
        }

        private void OnValidate()
        {
            shakeDuration = Mathf.Max(0f, shakeDuration);
            shakeVibrato = Mathf.Max(1, shakeVibrato);
            openDuration = Mathf.Max(0f, openDuration);
            closeDuration = Mathf.Max(0f, closeDuration);
        }
    }
}
