using UnityEngine;

namespace Member.KYM.Scripts.Players.RobotArm
{
    public class RobotArmFingerAnimator : MonoBehaviour
    {
        [Header("손가락 관절")]
        [SerializeField] private Transform upperLeft;
        [SerializeField] private Transform lowerLeft;
        [SerializeField] private Transform upperRight;
        [SerializeField] private Transform lowerRight;

        [Header("손가락 닫힘 각도")]
        [SerializeField] private float upperLeftClosedAngle = -25f;
        [SerializeField] private float lowerLeftClosedAngle = -35f;
        [SerializeField] private float upperRightClosedAngle = 25f;
        [SerializeField] private float lowerRightClosedAngle = 35f;

        [Header("손가락 동작 속도")]
        [SerializeField] private float fingerSmoothTime = 0.08f;
        [SerializeField] private float fingerOpenSmoothTime = 0.025f;

        private Quaternion _upperLeftRest;
        private Quaternion _lowerLeftRest;
        private Quaternion _upperRightRest;
        private Quaternion _lowerRightRest;

        private float _grabAmount;
        private float _grabVelocity;
        private bool _closed;

        private void Awake()
        {
            CacheRestPose();
        }

        private void Update()
        {
            float targetAmount = _closed ? 1f : 0f;
            float smoothTime = _closed
                ? fingerSmoothTime
                : fingerOpenSmoothTime;

            _grabAmount = Mathf.SmoothDamp(
                _grabAmount,
                targetAmount,
                ref _grabVelocity,
                smoothTime
            );

            ApplyFingerPose();
        }

        public void SetClosed(bool closed)
        {
            _closed = closed;
        }

        private void CacheRestPose()
        {
            if (upperLeft != null) _upperLeftRest = upperLeft.localRotation;
            if (lowerLeft != null) _lowerLeftRest = lowerLeft.localRotation;
            if (upperRight != null) _upperRightRest = upperRight.localRotation;
            if (lowerRight != null) _lowerRightRest = lowerRight.localRotation;
        }

        private void ApplyFingerPose()
        {
            ApplyRotation(upperLeft, _upperLeftRest, upperLeftClosedAngle);
            ApplyRotation(lowerLeft, _lowerLeftRest, lowerLeftClosedAngle);
            ApplyRotation(upperRight, _upperRightRest, upperRightClosedAngle);
            ApplyRotation(lowerRight, _lowerRightRest, lowerRightClosedAngle);
        }

        private void ApplyRotation(Transform bone, Quaternion restRotation, float closedAngle)
        {
            if (bone == null)
                return;

            bone.localRotation = restRotation
                * Quaternion.Euler(0f, 0f, closedAngle * _grabAmount);
        }

        private void OnValidate()
        {
            fingerSmoothTime = Mathf.Max(0.001f, fingerSmoothTime);
            fingerOpenSmoothTime = Mathf.Max(0.001f, fingerOpenSmoothTime);
        }
    }
}
