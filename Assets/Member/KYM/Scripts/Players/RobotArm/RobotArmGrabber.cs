using System.Collections;
using System.Collections.Generic;
using KimLIb.ModuleSystems;
using UnityEngine;

namespace Member.KYM.Scripts.Players.RobotArm
{
    public class RobotArmGrabber : MonoBehaviour
    {
        [Header("필수 참조")]
        [SerializeField] private ModuleOwner throwOwner;
        [SerializeField] private Transform grabPoint;
        [SerializeField] private Transform aimTarget;
        [SerializeField] private RobotArm robotArm;
        [SerializeField] private RobotArmFingerAnimator fingerAnimator;
        [SerializeField] private LineRenderer aimLine;

        [Header("잡기 설정")]
        [SerializeField] private float grabRadius = 0.15f;
        [SerializeField] private float grabYOffset = 0f;

        [Header("던지기 설정")]
        [SerializeField] private float throwSpeed = 10f;
        [SerializeField] private float throwReleaseDelay = 0.04f;

        [Header("잡기 연출")]
        [SerializeField] private float failedGrabCloseTime = 0.12f;

        [Header("투척 조준선")]
        [SerializeField] private float aimLineLength = 15f;
        [SerializeField] private LayerMask aimLineBlockLayers = ~0;

        public bool IsHolding => _heldObject != null;
        public bool IsBusy => _throwRoutine != null || _actionLocked;
        public Transform GrabPoint => grabPoint;

        private IGrabbable _heldObject;
        private Coroutine _failedGrabRoutine;
        private Coroutine _throwRoutine;
        private bool _actionLocked;
        private readonly List<RaycastHit2D> _aimHits = new List<RaycastHit2D>();

        private void Awake()
        {
            if (robotArm == null)
                robotArm = GetComponent<RobotArm>();

            if (GrabbableLayer.Index < 0)
            {
                Debug.LogError(
                    $"프로젝트에 {GrabbableLayer.Name} 레이어가 없습니다.",
                    this);
            }

            if (aimLine != null)
                aimLine.enabled = false;
        }

        private void LateUpdate()
        {
            if (aimLine == null)
                return;

            bool shouldShow = _heldObject != null &&
                              _heldObject.GrabTransform != null &&
                              grabPoint != null &&
                              aimTarget != null;
            aimLine.enabled = shouldShow;
            if (!shouldShow)
                return;

            Vector2 origin = grabPoint.position;
            Vector2 direction = GetAimDirection();
            Vector2 end = origin + direction * aimLineLength;

            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(aimLineBlockLayers);
            filter.useTriggers = false;
            Physics2D.Raycast(origin, direction, filter, _aimHits, aimLineLength);

            foreach (RaycastHit2D hit in _aimHits)
            {
                Collider2D collider = hit.collider;
                if (collider == null ||
                    (throwOwner != null && collider.transform.IsChildOf(throwOwner.transform)) ||
                    collider.transform.IsChildOf(_heldObject.GrabTransform))
                {
                    continue;
                }

                end = hit.point;
                break;
            }

            aimLine.SetPosition(0, new Vector3(origin.x, origin.y, grabPoint.position.z));
            aimLine.SetPosition(1, new Vector3(end.x, end.y, grabPoint.position.z));
        }

        private void OnDisable()
        {
            _actionLocked = false;

            if (aimLine != null)
                aimLine.enabled = false;

            if (_throwRoutine != null)
            {
                StopCoroutine(_throwRoutine);
                _throwRoutine = null;
            }
        }

        public void Release()
        {
            if (_heldObject == null)
                return;

            if (_throwRoutine != null)
            {
                StopCoroutine(_throwRoutine);
                _throwRoutine = null;
            }

            ReleaseHeldObject();
        }

        public bool TryThrow()
        {
            if (_heldObject == null || _throwRoutine != null)
                return false;

            _throwRoutine = StartCoroutine(ThrowRoutine());
            return true;
        }

        private IEnumerator ThrowRoutine()
        {
            Vector2 throwDirection = GetAimDirection();

            fingerAnimator?.SetClosed(false);
            yield return new WaitForSeconds(throwReleaseDelay);

            if (_heldObject == null)
            {
                _throwRoutine = null;
                yield break;
            }

            ThrowData context = new ThrowData(
                throwDirection,
                throwOwner,
                throwSpeed
            );

            IGrabbable objectToThrow = _heldObject;
            _heldObject = null;

            robotArm?.ApplyRecoil(throwDirection);
            objectToThrow.Throw(context);
            _throwRoutine = null;
        }

        public bool TryGrab()
        {
            IGrabbable nearest = FindNearestGrabbable();
            return TryGrab(nearest);
        }

        public bool TryGrab(IGrabbable grabbable)
        {
            if (_heldObject != null ||
                _throwRoutine != null ||
                grabbable == null ||
                grabbable.GrabTransform == null ||
                !grabbable.CanBeGrabbed)
            {
                return false;
            }

            _heldObject = grabbable;
            _heldObject.Grab(grabPoint, throwOwner != null ? throwOwner.gameObject : null);
            fingerAnimator?.SetClosed(true);
            return true;
        }

        public void SetActionLocked(bool locked)
        {
            _actionLocked = locked;
        }

        private IGrabbable FindNearestGrabbable()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(grabPoint.position + (Vector3.up * grabYOffset)
                , grabRadius, GrabbableLayer.Mask
            );

            IGrabbable nearest = null;
            float nearestSqrDistance = float.PositiveInfinity;

            foreach (Collider2D hit in hits)
            {
                IGrabbable candidate = FindGrabbable(hit);
                if (candidate == null || !candidate.CanBeGrabbed)
                    continue;

                float sqrDistance = (
                    candidate.GrabTransform.position - grabPoint.position
                ).sqrMagnitude;

                if (sqrDistance >= nearestSqrDistance)
                    continue;

                nearest = candidate;
                nearestSqrDistance = sqrDistance;
            }

            return nearest;
        }

        private static IGrabbable FindGrabbable(Collider2D hit)
        {
            MonoBehaviour[] behaviours = hit.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IGrabbable grabbable)
                    return grabbable;
            }

            return null;
        }

        private void ReleaseHeldObject()
        {
            IGrabbable objectToRelease = _heldObject;
            _heldObject = null;

            objectToRelease.Release();
            fingerAnimator?.SetClosed(false);
        }

        private Vector2 GetAimDirection()
        {
            Vector2 direction = aimTarget.right;
            return direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector2.right;
        }

        public void PlayFailedAction()
        {
            if (_failedGrabRoutine != null)
                StopCoroutine(_failedGrabRoutine);

            _failedGrabRoutine = StartCoroutine(FailedGrabRoutine());
        }

        private IEnumerator FailedGrabRoutine()
        {
            fingerAnimator?.SetClosed(true);
            yield return new WaitForSeconds(failedGrabCloseTime);
            fingerAnimator?.SetClosed(false);
            _failedGrabRoutine = null;
        }

        private void OnDrawGizmosSelected()
        {
            if (grabPoint == null)
                return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(grabPoint.position + (Vector3.up * grabYOffset), grabRadius);
        }

        private void OnValidate()
        {
            grabRadius = Mathf.Max(0f, grabRadius);
            throwSpeed = Mathf.Max(0f, throwSpeed);
            throwReleaseDelay = Mathf.Max(0f, throwReleaseDelay);
            failedGrabCloseTime = Mathf.Max(0f, failedGrabCloseTime);
            aimLineLength = Mathf.Max(0f, aimLineLength);
        }
    }
}
