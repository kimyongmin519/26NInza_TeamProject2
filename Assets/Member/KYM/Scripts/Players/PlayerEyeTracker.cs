using Member.KYM.Scripts.CoreSystems;
using UnityEngine;

namespace Member.KYM.Scripts.Players
{
    public class PlayerEyeTracker : MonoBehaviour
    {
        [SerializeField] private PlayerInputSO playerInput;
        [SerializeField] private Vector2 maxOffset;
        [SerializeField] private float smoothTime = 0.08f;

        private Camera _mainCamera;
        private Transform _eyeParent;
        private Vector3 _restLocalPosition;
        private Vector3 _smoothVelocity;

        private void Awake()
        {
            _mainCamera = Camera.main;
            _eyeParent = transform.parent;
            _restLocalPosition = transform.localPosition;
        }

        private void LateUpdate()
        {
            if (playerInput == null || _mainCamera == null || _eyeParent == null)
                return;

            Vector3 screenPosition = playerInput.MousePos;
            screenPosition.z = Mathf.Abs(
                _mainCamera.transform.position.z - transform.position.z
            );

            Vector3 mouseWorldPosition =
                _mainCamera.ScreenToWorldPoint(screenPosition);

            Vector3 mouseLocalPosition =
                _eyeParent.InverseTransformPoint(mouseWorldPosition);

            Vector2 lookDirection =
                (Vector2)(mouseLocalPosition - _restLocalPosition);

            Vector2 offset = lookDirection.sqrMagnitude > 0.0001f
                ? Vector2.Scale(lookDirection.normalized, maxOffset)
                : Vector2.zero;

            Vector3 targetLocalPosition = _restLocalPosition
                + new Vector3(offset.x, offset.y, 0f);

            transform.localPosition = Vector3.SmoothDamp(
                transform.localPosition,
                targetLocalPosition,
                ref _smoothVelocity,
                smoothTime
            );
        }

        private void OnValidate()
        {
            maxOffset.x = Mathf.Max(0f, maxOffset.x);
            maxOffset.y = Mathf.Max(0f, maxOffset.y);
            smoothTime = Mathf.Max(0.001f, smoothTime);
        }
    }
}
