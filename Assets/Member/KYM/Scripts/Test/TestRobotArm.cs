using Core;
using UnityEngine;

namespace Member.KYM.Scripts.Test
{
    public class TestRobotArm : MonoBehaviour
    {
        [SerializeField] private PlayerInputSO playerInput;
        [SerializeField] private Transform armBase;
        [SerializeField] private Transform handBone;
        [SerializeField] private Transform armTarget;

        [SerializeField] private float smoothTime = 0.04f;
        [SerializeField] private float rotationOffset;
        [SerializeField] private LayerMask obstacleLayers;
        [SerializeField] private float handRadius = 0.08f;
        [SerializeField] private float surfaceOffset = 0.02f;

        private Vector3 _positionVelocity;
        private float _rotationVelocity;
        private Camera _mainCamera;

        private void Awake()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;
        }

        private void Update()
        {
            if (playerInput == null ||
                _mainCamera == null ||
                armBase == null ||
                handBone == null ||
                armTarget == null)
            {
                return;
            }

            Vector3 screenPosition = playerInput.MousePos;

            screenPosition.z =
                Mathf.Abs(_mainCamera.transform.position.z - armTarget.position.z);

            Vector3 mouseWorldPosition =
                _mainCamera.ScreenToWorldPoint(screenPosition);

            mouseWorldPosition.z = armTarget.position.z;

            Vector2 safeMousePosition = ClampTargetToObstacles(
                armBase.position,
                mouseWorldPosition
            );

            Vector3 desiredTargetPosition = new Vector3(
                safeMousePosition.x,
                safeMousePosition.y,
                armTarget.position.z
            );

            Vector3 smoothedTargetPosition = Vector3.SmoothDamp(
                armTarget.position,
                desiredTargetPosition,
                ref _positionVelocity,
                smoothTime
            );

            // SmoothDamp can cut across a corner, so validate the smoothed
            // position once more before applying it.
            Vector2 safeSmoothedPosition = ClampTargetToObstacles(
                armBase.position,
                smoothedTargetPosition
            );

            armTarget.position = new Vector3(
                safeSmoothedPosition.x,
                safeSmoothedPosition.y,
                armTarget.position.z
            );

            // Aim from the hand's current position toward the mouse.
            Vector2 aimDirection =
                mouseWorldPosition - handBone.position;

            if (aimDirection.sqrMagnitude < 0.0001f)
                return;

            float targetAngle =
                Mathf.Atan2(aimDirection.y, aimDirection.x)
                * Mathf.Rad2Deg
                + rotationOffset;

            float smoothAngle = Mathf.SmoothDampAngle(
                armTarget.eulerAngles.z,
                targetAngle,
                ref _rotationVelocity,
                smoothTime
            );

            armTarget.rotation =
                Quaternion.Euler(0f, 0f, smoothAngle);
        }

        private Vector2 ClampTargetToObstacles(Vector2 origin, Vector2 desiredPosition)
        {
            Vector2 toTarget = desiredPosition - origin;
            float distance = toTarget.magnitude;

            if (distance <= 0.0001f)
                return desiredPosition;

            Vector2 direction = toTarget / distance;
            RaycastHit2D hit = Physics2D.CircleCast(
                origin,
                handRadius,
                direction,
                distance,
                obstacleLayers
            );

            if (hit.collider == null)
                return desiredPosition;

            return hit.centroid - direction * surfaceOffset;
        }

        private void OnValidate()
        {
            smoothTime = Mathf.Max(0.001f, smoothTime);
            handRadius = Mathf.Max(0f, handRadius);
            surfaceOffset = Mathf.Max(0f, surfaceOffset);
        }
    }
}
