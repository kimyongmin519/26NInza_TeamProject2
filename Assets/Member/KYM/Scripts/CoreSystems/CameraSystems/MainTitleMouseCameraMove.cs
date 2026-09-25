using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Member.KYM.Scripts.CoreSystems.CameraSystems
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineCamera))]
    public sealed class MainTitleMouseCameraMove : MonoBehaviour
    {
        [Header("마우스에 따른 카메라 최대 이동량")]
        [SerializeField] private Vector2 maxOffset = new(0.22f, 0.12f);
        [SerializeField, Min(0f)] private float smoothTime = 0.35f;

        private Vector3 _baseLocalPosition;
        private Vector2 _velocity;

        private void Awake()
        {
            _baseLocalPosition = transform.localPosition;
        }

        private void Update()
        {
            if (Screen.width <= 0 || Screen.height <= 0)
                return;

            Vector2 mousePosition = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            Vector2 normalizedPosition = new(
                Mathf.Clamp(mousePosition.x / Screen.width * 2f - 1f, -1f, 1f),
                Mathf.Clamp(mousePosition.y / Screen.height * 2f - 1f, -1f, 1f));

            Vector2 targetOffset = Vector2.Scale(normalizedPosition, maxOffset);
            Vector2 currentOffset = (Vector2)(transform.localPosition - _baseLocalPosition);
            Vector2 nextOffset = smoothTime <= 0f
                ? targetOffset
                : Vector2.SmoothDamp(
                    currentOffset, targetOffset, ref _velocity,
                    smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

            transform.localPosition = _baseLocalPosition + (Vector3)nextOffset;
        }

        private void OnDisable()
        {
            transform.localPosition = _baseLocalPosition;
            _velocity = Vector2.zero;
        }

        private void OnValidate()
        {
            maxOffset = new Vector2(
                Mathf.Max(0f, maxOffset.x),
                Mathf.Max(0f, maxOffset.y));
            smoothTime = Mathf.Max(0f, smoothTime);
        }
    }
}
