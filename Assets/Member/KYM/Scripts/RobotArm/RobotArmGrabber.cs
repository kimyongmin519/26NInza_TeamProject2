using System.Collections;
using Core;
using UnityEngine;

namespace Member.KYM.Scripts.RobotArm
{
    public class RobotArmGrabber : MonoBehaviour
    {
        [SerializeField] private PlayerInputSO playerInput;
        [SerializeField] private GameObject throwOwner;
        [SerializeField] private Transform grabPoint;
        [SerializeField] private Transform aimTarget;
        [SerializeField] private RobotArmFingerAnimator fingerAnimator;

        [SerializeField] private LayerMask grabbableLayers = ~0;
        [SerializeField] private float grabRadius = 0.15f;
        [SerializeField] private float throwSpeed = 10f;
        [SerializeField] private float failedGrabCloseTime = 0.12f;

        public bool IsHolding => _heldObject != null;

        private IGrabbable _heldObject;
        private Coroutine _failedGrabRoutine;

        private void OnEnable()
        {
            if (playerInput == null)
                return;

            playerInput.InteractPressed += HandleInteract;
            playerInput.AttackPressed += HandleAttack;
        }

        private void OnDisable()
        {
            if (playerInput == null)
                return;

            playerInput.InteractPressed -= HandleInteract;
            playerInput.AttackPressed -= HandleAttack;
        }

        private void HandleInteract()
        {
            if (_heldObject != null)
            {
                ReleaseHeldObject();
                return;
            }

            TryGrabNearest();
        }

        private void HandleAttack()
        {
            if (_heldObject == null)
                return;

            Vector2 throwDirection = GetAimDirection();
            IGrabbable objectToThrow = _heldObject;
            _heldObject = null;

            objectToThrow.Throw(throwDirection * throwSpeed, throwOwner);
            fingerAnimator?.SetClosed(false);
        }

        private void TryGrabNearest()
        {
            IGrabbable nearest = FindNearestGrabbable();
            if (nearest == null)
            {
                PlayFailedGrabAnimation();
                return;
            }

            _heldObject = nearest;
            _heldObject.Grab(grabPoint, throwOwner);
            fingerAnimator?.SetClosed(true);
        }

        private IGrabbable FindNearestGrabbable()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                grabPoint.position,
                grabRadius,
                grabbableLayers
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
            Vector2 direction = aimTarget.position - grabPoint.position;
            return direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector2.right;
        }

        private void PlayFailedGrabAnimation()
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
            Gizmos.DrawWireSphere(grabPoint.position, grabRadius);
        }

        private void OnValidate()
        {
            grabRadius = Mathf.Max(0f, grabRadius);
            throwSpeed = Mathf.Max(0f, throwSpeed);
            failedGrabCloseTime = Mathf.Max(0f, failedGrabCloseTime);
        }
    }
}
