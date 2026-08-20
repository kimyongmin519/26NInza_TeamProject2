using System;
using System.Collections;
using Member.KYM.Scripts.Agents;
using UnityEngine;

namespace Member.KYM.Scripts.Players.RobotArm
{
    public class RobotArmGrappler : MonoBehaviour
    {
        [SerializeField] private RobotArm robotArm;
        [SerializeField] private RobotArmFingerAnimator fingerAnimator;
        [SerializeField] private LayerMask grappleLayers = ~0;
        [SerializeField] private float detectionRadius = 0.35f;
        [SerializeField] private float minimumHangDistance = 0.25f;
        [SerializeField, Range(0f, 2f)] private float hangGravityScale = 1f;
        [SerializeField] private float swingForce = 30f;
        [SerializeField] private float maximumSwingSpeed = 12f;
        [SerializeField] private float controlReturnDelay = 0.12f;

        public event Action GrappleStarted;
        public event Action GrappleEnded;

        public bool IsGrappling => _currentAnchor != null;

        private IGrappleAnchor _currentAnchor;
        private IMover _mover;
        private PlayerController _player;
        private GameObject _owner;
        private DistanceJoint2D _joint;
        private Rigidbody2D _connectedBody;
        private Coroutine _controlReturnRoutine;

        private void Awake()
        {
            if (robotArm == null)
                robotArm = GetComponent<RobotArm>();

            if (fingerAnimator == null)
                fingerAnimator = GetComponent<RobotArmFingerAnimator>();
        }

        private void FixedUpdate()
        {
            if (!IsGrappling)
                return;

            if (!TryGetAnchorPoint(out Transform point))
            {
                StopGrappleInternal(false);
                return;
            }

            UpdateConnectedAnchor(point);
            ApplySwingForce(point.position);
        }

        private void OnDisable()
        {
            if (_currentAnchor != null)
                StopGrappleInternal(false);

            if (_controlReturnRoutine != null)
            {
                StopCoroutine(_controlReturnRoutine);
                _controlReturnRoutine = null;
            }

            RestoreMovement();
        }

        public bool TryStartNearest(Vector2 searchPosition)
        {
            if (IsGrappling)
                return false;

            IGrappleAnchor anchor = FindNearestAnchor(searchPosition);
            return TryStartGrapple(anchor);
        }

        public bool TryStartGrapple(IGrappleAnchor anchor)
        {
            if (anchor == null || !anchor.CanGrapple || !ResolveMover())
                return false;

            Transform point = anchor.GrapplePoint;
            if (point == null)
                return false;

            if (_controlReturnRoutine != null)
            {
                StopCoroutine(_controlReturnRoutine);
                _controlReturnRoutine = null;
            }

            _currentAnchor = anchor;
            ConfigureJoint(point);

            _mover.CanManualMovement = false;
            _mover.SetMovementX(0f);
            _mover.SetGravityScale(hangGravityScale);
            robotArm.SetTargetOverride(point);
            fingerAnimator?.SetClosed(true);

            anchor.OnGrappleStarted(_owner);
            GrappleStarted?.Invoke();
            return true;
        }

        public void StopGrapple()
        {
            StopGrappleInternal(true);
        }

        private void StopGrappleInternal(bool delayControl)
        {
            IGrappleAnchor anchor = _currentAnchor;
            if (anchor == null)
                return;

            _currentAnchor = null;
            DisableJoint();
            robotArm.ClearTargetOverride();
            fingerAnimator?.SetClosed(false);

            if (_mover != null)
            {
                _mover.SetGravityScale(1f);

                if (delayControl && controlReturnDelay > 0f && isActiveAndEnabled)
                {
                    _controlReturnRoutine = StartCoroutine(
                        RestoreManualControlRoutine()
                    );
                }
                else
                {
                    _mover.CanManualMovement = true;
                }
            }

            if (IsAnchorAlive(anchor))
                anchor.OnGrappleEnded(_owner);

            GrappleEnded?.Invoke();
        }

        private void ConfigureJoint(Transform point)
        {
            if (_joint == null)
                _joint = _owner.AddComponent<DistanceJoint2D>();

            _connectedBody = point.GetComponentInParent<Rigidbody2D>();
            _joint.enabled = false;
            _joint.autoConfigureConnectedAnchor = false;
            _joint.autoConfigureDistance = false;
            _joint.connectedBody = _connectedBody;
            _joint.anchor = _mover.RigidBody.transform.InverseTransformPoint(
                robotArm.ArmBasePosition
            );
            _joint.maxDistanceOnly = true;
            _joint.enableCollision = false;
            UpdateConnectedAnchor(point);

            float distance = Vector2.Distance(
                robotArm.ArmBasePosition,
                point.position
            );

            _joint.distance = Mathf.Max(minimumHangDistance, distance);
            _joint.enabled = true;
        }

        private void UpdateConnectedAnchor(Transform point)
        {
            if (_joint == null)
                return;

            if (_connectedBody != null)
            {
                _joint.connectedAnchor =
                    _connectedBody.transform.InverseTransformPoint(
                        point.position
                    );
            }
            else
            {
                _joint.connectedAnchor = point.position;
            }
        }

        private void DisableJoint()
        {
            if (_joint == null)
                return;

            _joint.enabled = false;
            _joint.connectedBody = null;
            _connectedBody = null;
        }

        private void ApplySwingForce(Vector2 anchorPosition)
        {
            if (_mover == null || _player == null || _player.PlayerInput == null)
                return;

            float input = _player.PlayerInput.MoveDirX;
            if (Mathf.Abs(input) < 0.01f)
                return;

            Rigidbody2D body = _mover.RigidBody;
            Vector2 fromAnchor = robotArm.ArmBasePosition - anchorPosition;
            if (fromAnchor.sqrMagnitude < 0.0001f)
                return;

            Vector2 tangent = new Vector2(
                -fromAnchor.y,
                fromAnchor.x
            ).normalized;

            if (tangent.x * input < 0f)
                tangent = -tangent;

            float tangentSpeed = Vector2.Dot(
                body.linearVelocity,
                tangent
            );

            if (tangentSpeed >= maximumSwingSpeed)
                return;

            body.AddForce(tangent * (Mathf.Abs(input) * swingForce));
        }

        private IEnumerator RestoreManualControlRoutine()
        {
            yield return new WaitForSeconds(controlReturnDelay);
            RestoreMovement();
            _controlReturnRoutine = null;
        }

        private void RestoreMovement()
        {
            if (_mover == null)
                return;

            _mover.SetGravityScale(1f);
            _mover.CanManualMovement = true;
        }

        private IGrappleAnchor FindNearestAnchor(Vector2 searchPosition)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                searchPosition,
                detectionRadius,
                grappleLayers
            );

            IGrappleAnchor nearest = null;
            float nearestSqrDistance = float.PositiveInfinity;

            foreach (Collider2D hit in hits)
            {
                MonoBehaviour[] behaviours =
                    hit.GetComponentsInParent<MonoBehaviour>();

                foreach (MonoBehaviour behaviour in behaviours)
                {
                    if (behaviour is not IGrappleAnchor anchor ||
                        !anchor.CanGrapple ||
                        anchor.GrapplePoint == null)
                    {
                        continue;
                    }

                    float sqrDistance = (
                        (Vector2)anchor.GrapplePoint.position - searchPosition
                    ).sqrMagnitude;

                    if (sqrDistance >= nearestSqrDistance)
                        continue;

                    nearest = anchor;
                    nearestSqrDistance = sqrDistance;
                }
            }

            return nearest;
        }

        private bool ResolveMover()
        {
            if (_mover != null)
                return true;

            _player = GetComponentInParent<PlayerController>();
            if (_player == null)
                return false;

            _mover = _player.GetModule<IMover>();
            _owner = _player.gameObject;
            return _mover != null;
        }

        private bool TryGetAnchorPoint(out Transform point)
        {
            point = null;
            if (!IsAnchorAlive(_currentAnchor))
                return false;

            point = _currentAnchor.GrapplePoint;
            return point != null;
        }

        private static bool IsAnchorAlive(IGrappleAnchor anchor)
        {
            return anchor is MonoBehaviour behaviour && behaviour != null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }

        private void OnValidate()
        {
            detectionRadius = Mathf.Max(0f, detectionRadius);
            minimumHangDistance = Mathf.Max(0.01f, minimumHangDistance);
            hangGravityScale = Mathf.Max(0f, hangGravityScale);
            swingForce = Mathf.Max(0f, swingForce);
            maximumSwingSpeed = Mathf.Max(0f, maximumSwingSpeed);
            controlReturnDelay = Mathf.Max(0f, controlReturnDelay);
        }
    }
}
