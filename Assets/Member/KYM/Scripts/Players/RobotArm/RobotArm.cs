using Member.KYM.Scripts.CoreSystems;
using UnityEngine;

namespace Member.KYM.Scripts.Players.RobotArm
{
    public class RobotArm : MonoBehaviour
    {
        [SerializeField] private PlayerInputSO playerInput;
        [SerializeField] private Transform armBase;
        [SerializeField] private Transform handBone;
        [SerializeField] private Transform grabCenter;
        [SerializeField] private Transform armTarget;

        [SerializeField] private float smoothTime = 0.04f;
        [SerializeField] private float rotationOffset;
        [SerializeField, Range(0f, 180f)] private float handRotationLimit = 90f;
        [SerializeField] private LayerMask obstacleLayers;
        [SerializeField] private float handRadius = 0.08f;
        [SerializeField] private float surfaceOffset = 0.02f;
        [SerializeField] private float recoilDistance = 0.35f;
        [SerializeField] private float recoilRecoveryTime = 0.12f;

        private Vector3 _positionVelocity;
        private Vector2 _recoilOffset;
        private Vector2 _recoilVelocity;
        private float _rotationVelocity;
        private float _handRestLocalAngle;
        private float _lastAimAngle;
        private float _maxArmReach;
        private Transform _targetOverride;
        private Camera _mainCamera;

        public Vector2 ArmBasePosition => armBase.position;
        public float MaximumGrabReach
        {
            get
            {
                float grabOffset = grabCenter != null
                    ? Vector2.Distance(grabCenter.position, handBone.position)
                    : 0f;

                return _maxArmReach + grabOffset;
            }
        }

        private void Awake()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;
            
            _handRestLocalAngle = Mathf.DeltaAngle(0f, handBone.localEulerAngles.z);
                
            _lastAimAngle = armTarget.eulerAngles.z;
            _maxArmReach = CalculateMaxArmReach();
        }

        private void Update()
        {
            Vector3 screenPosition = playerInput.MousePos;

            screenPosition.z =
                Mathf.Abs(_mainCamera.transform.position.z - armTarget.position.z);

            Vector3 mouseWorldPosition =
                _mainCamera.ScreenToWorldPoint(screenPosition);

            mouseWorldPosition.z = armTarget.position.z;

            Vector3 desiredWorldPosition = _targetOverride != null
                ? _targetOverride.position
                : mouseWorldPosition;

            desiredWorldPosition.z = armTarget.position.z;

            _recoilOffset = Vector2.SmoothDamp(
                _recoilOffset,
                Vector2.zero,
                ref _recoilVelocity,
                recoilRecoveryTime
            );

            Vector2 aimDirection =
                (Vector2)desiredWorldPosition - (Vector2)armBase.position;

            if (aimDirection.sqrMagnitude >= 0.0001f)
            {
                _lastAimAngle =
                    Mathf.Atan2(aimDirection.y, aimDirection.x)
                    * Mathf.Rad2Deg
                    + rotationOffset;
            }

            float targetAngle = ClampHandWorldAngle(_lastAimAngle);

            float smoothAngle = Mathf.SmoothDampAngle(
                armTarget.eulerAngles.z,
                targetAngle,
                ref _rotationVelocity,
                smoothTime
            );

            smoothAngle = ClampHandWorldAngle(smoothAngle);

            Vector2 desiredHandPosition = GetHandTargetPosition(
                desiredWorldPosition,
                smoothAngle
            );

            desiredHandPosition =
                ClampTargetToArmReach(desiredHandPosition) + _recoilOffset;

            desiredHandPosition = ClampTargetToArmReach(
                desiredHandPosition
            );

            Vector2 safeHandPosition = ClampTargetToObstacles(
                armBase.position,
                desiredHandPosition
            );

            Vector3 desiredTargetPosition = new Vector3(
                safeHandPosition.x,
                safeHandPosition.y,
                armTarget.position.z
            );

            Vector3 smoothedTargetPosition = _targetOverride != null
                ? desiredTargetPosition
                : Vector3.SmoothDamp(
                    armTarget.position,
                    desiredTargetPosition,
                    ref _positionVelocity,
                    smoothTime
                );

            Vector2 reachableSmoothedPosition = ClampTargetToArmReach(
                smoothedTargetPosition
            );

            Vector2 safeSmoothedPosition = ClampTargetToObstacles(
                armBase.position,
                reachableSmoothedPosition
            );

            armTarget.position = new Vector3(
                safeSmoothedPosition.x,
                safeSmoothedPosition.y,
                armTarget.position.z
            );

            armTarget.rotation =
                Quaternion.Euler(0f, 0f, smoothAngle);
        }

        public void ApplyRecoil(Vector2 shotDirection)
        {
            if (shotDirection.sqrMagnitude < 0.0001f)
                return;

            _recoilOffset =
                -shotDirection.normalized * recoilDistance;
            _recoilVelocity = Vector2.zero;
            _positionVelocity = Vector3.zero;

            Vector2 recoilPosition =
                (Vector2)armTarget.position + _recoilOffset;

            recoilPosition = ClampTargetToArmReach(recoilPosition);

            armTarget.position = new Vector3(
                recoilPosition.x,
                recoilPosition.y,
                armTarget.position.z
            );
        }

        public void SetTargetOverride(Transform target)
        {
            _targetOverride = target;
            _positionVelocity = Vector3.zero;
        }

        public void ClearTargetOverride()
        {
            _targetOverride = null;
            _positionVelocity = Vector3.zero;
        }

        private float CalculateMaxArmReach()
        {
            Transform lowerArm = handBone.parent;
            if (lowerArm == null)
            {
                return Vector2.Distance(
                    armBase.position,
                    handBone.position
                );
            }

            float upperLength = Vector2.Distance(
                armBase.position,
                lowerArm.position
            );

            float lowerLength = Vector2.Distance(
                lowerArm.position,
                handBone.position
            );

            return upperLength + lowerLength;
        }

        private Vector2 ClampTargetToArmReach(Vector2 desiredPosition)
        {
            Vector2 origin = armBase.position;
            Vector2 offset = desiredPosition - origin;

            if (_maxArmReach <= 0f ||
                offset.sqrMagnitude <= _maxArmReach * _maxArmReach)
            {
                return desiredPosition;
            }

            return origin + offset.normalized * _maxArmReach;
        }

        private Vector2 GetHandTargetPosition(
            Vector2 desiredGrabCenterPosition,
            float desiredHandWorldAngle
        )
        {
            if (grabCenter == null || grabCenter == handBone)
                return desiredGrabCenterPosition;

            Vector2 currentOffset =
                grabCenter.position - handBone.position;

            float rotationDelta = Mathf.DeltaAngle(
                handBone.eulerAngles.z,
                desiredHandWorldAngle
            );

            Vector3 desiredOffset =
                Quaternion.Euler(0f, 0f, rotationDelta) * currentOffset;

            return desiredGrabCenterPosition - (Vector2)desiredOffset;
        }

        private float ClampHandWorldAngle(float desiredWorldAngle)
        {
            if (handBone.parent == null)
                return desiredWorldAngle;

            float centerAngle =
                handBone.parent.eulerAngles.z + _handRestLocalAngle;

            float angleFromCenter = Mathf.DeltaAngle(
                centerAngle,
                desiredWorldAngle
            );

            float limitedAngle = Mathf.Clamp(
                angleFromCenter,
                -handRotationLimit,
                handRotationLimit
            );

            return centerAngle + limitedAngle;
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
            handRotationLimit = Mathf.Clamp(handRotationLimit, 0f, 180f);
            handRadius = Mathf.Max(0f, handRadius);
            surfaceOffset = Mathf.Max(0f, surfaceOffset);
            recoilDistance = Mathf.Max(0f, recoilDistance);
            recoilRecoveryTime = Mathf.Max(0.001f, recoilRecoveryTime);
        }
    }
}
