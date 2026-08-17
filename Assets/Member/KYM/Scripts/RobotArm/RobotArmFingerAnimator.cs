using UnityEngine;

namespace Member.KYM.Scripts.RobotArm
{
    public class RobotArmFingerAnimator : MonoBehaviour
    {
        [Header("Bone names")]
        [SerializeField] private string upperLeftName = "Upper_Left_Finger_Bone";
        [SerializeField] private string lowerLeftName = "Lower_Left_Finger_Bone";
        [SerializeField] private string upperRightName = "Upper_Right_Finger_Bone";
        [SerializeField] private string lowerRightName = "Lower_Right_Finger_Bone";

        [Header("Closed rotation offsets")]
        [SerializeField] private float upperLeftClosedAngle = -25f;
        [SerializeField] private float lowerLeftClosedAngle = -35f;
        [SerializeField] private float upperRightClosedAngle = 25f;
        [SerializeField] private float lowerRightClosedAngle = 35f;
        [SerializeField] private float fingerSmoothTime = 0.08f;

        private Transform _upperLeft;
        private Transform _lowerLeft;
        private Transform _upperRight;
        private Transform _lowerRight;

        private Quaternion _upperLeftRest;
        private Quaternion _lowerLeftRest;
        private Quaternion _upperRightRest;
        private Quaternion _lowerRightRest;

        private float _grabAmount;
        private float _grabVelocity;
        private bool _closed;

        private void Awake()
        {
            _upperLeft = FindChildRecursive(transform, upperLeftName);
            _lowerLeft = FindChildRecursive(transform, lowerLeftName);
            _upperRight = FindChildRecursive(transform, upperRightName);
            _lowerRight = FindChildRecursive(transform, lowerRightName);

            CacheRestPose();
        }

        private void Update()
        {
            float targetAmount = _closed ? 1f : 0f;
            _grabAmount = Mathf.SmoothDamp(
                _grabAmount,
                targetAmount,
                ref _grabVelocity,
                fingerSmoothTime
            );

            ApplyFingerPose();
        }

        public void SetClosed(bool closed)
        {
            _closed = closed;
        }

        private void CacheRestPose()
        {
            if (_upperLeft != null) _upperLeftRest = _upperLeft.localRotation;
            if (_lowerLeft != null) _lowerLeftRest = _lowerLeft.localRotation;
            if (_upperRight != null) _upperRightRest = _upperRight.localRotation;
            if (_lowerRight != null) _lowerRightRest = _lowerRight.localRotation;
        }

        private void ApplyFingerPose()
        {
            ApplyRotation(_upperLeft, _upperLeftRest, upperLeftClosedAngle);
            ApplyRotation(_lowerLeft, _lowerLeftRest, lowerLeftClosedAngle);
            ApplyRotation(_upperRight, _upperRightRest, upperRightClosedAngle);
            ApplyRotation(_lowerRight, _lowerRightRest, lowerRightClosedAngle);
        }

        private void ApplyRotation(Transform bone, Quaternion restRotation, float closedAngle)
        {
            if (bone == null)
                return;

            bone.localRotation = restRotation
                * Quaternion.Euler(0f, 0f, closedAngle * _grabAmount);
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            foreach (Transform child in root)
            {
                if (child.name == childName)
                    return child;

                Transform result = FindChildRecursive(child, childName);
                if (result != null)
                    return result;
            }

            return null;
        }

        private void OnValidate()
        {
            fingerSmoothTime = Mathf.Max(0.001f, fingerSmoothTime);
        }
    }
}
