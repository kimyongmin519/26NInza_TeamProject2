using System;
using KimLIb.ModuleSystems;
using UnityEngine;

namespace Member.KYM.Scripts.Agents
{
    public class AgentSensor : MonoBehaviour, IModule
    {
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private LayerMask interactableLayer;
        [SerializeField] private LayerMask targetLayer;

        [SerializeField] private Vector2 boxSize;
        [SerializeField] private Vector2 boxOffset;

        [Header("땅 체크")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private Vector2 groundCheckSize;
        [SerializeField] private Vector2 groundCheckOffset;

        [SerializeField] private int maxColliderCount = 5;
        private Collider[] _colliderResults;
        public Collider[] ColliderResults => _colliderResults;

        public bool IsGrounded { get; private set; }
        public event Action<bool> OnGroundStatusChange;

        private ModuleOwner _owner;

        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
        }

        private void FixedUpdate()
        {
            CheckGround();
        }

        private void CheckGround()
        {
            bool isGrounded = Physics2D.OverlapBox(
                GetGroundCheckPosition(),
                groundCheckSize,
                0f,
                groundLayer);

            if (IsGrounded == isGrounded)
                return;

            IsGrounded = isGrounded;
            OnGroundStatusChange?.Invoke(IsGrounded);
        }

        public bool TryGetOneWayPlatformBelow(out Collider2D platformCollider)
        {
            Collider2D[] groundColliders = Physics2D.OverlapBoxAll(
                GetGroundCheckPosition(),
                groundCheckSize,
                0f,
                groundLayer);

            foreach (Collider2D groundCollider in groundColliders)
            {
                if (groundCollider == null || !groundCollider.usedByEffector)
                    continue;

                PlatformEffector2D platformEffector = groundCollider.GetComponentInParent<PlatformEffector2D>();
                if (platformEffector != null && platformEffector.useOneWay)
                {
                    platformCollider = groundCollider;
                    return true;
                }
            }

            platformCollider = null;
            return false;
        }

        private Vector2 GetGroundCheckPosition()
        {
            return (Vector2)transform.position + groundCheckOffset;
        }

        public bool IsObstaclePresent(Vector2 direction, out Collider2D hitCollider)
        {
            Vector2 position = (Vector2)transform.position + direction + boxOffset;
            hitCollider = Physics2D.OverlapBox(position, boxSize, 0, obstacleLayer);
            return hitCollider != null;
        }
        
        public float BoxCastObstacle(Vector2 direction, float distance, out RaycastHit2D hit)
        {
            hit = Physics2D.BoxCast((Vector2)transform.position + boxOffset, boxSize, 0, direction, distance, obstacleLayer);
            distance = hit ? hit.distance : distance;
            return distance;
        }

        public bool IsTargetInRange(float range, out Collider2D hitCollider)
        {
            hitCollider = Physics2D.OverlapCircle(transform.position, range, targetLayer);
            return hitCollider != null;
        }

        public bool IsTargetInSight(Vector3 startPosition, float range, Collider2D target)
        {
            Vector2 direction = target.transform.position - startPosition;
            RaycastHit2D hit = Physics2D.Raycast(startPosition, direction.normalized, direction.magnitude, obstacleLayer);
            return hit.collider == null; //타겟과 나 사이에 아무런 장애물이 없을 경우 null이 나온다.
        }
        public bool IsInteractableInDirection()
        {

            Collider2D Target = Physics2D.OverlapBox(transform.position, boxSize, 0, interactableLayer);

            if (Target == null)
                return false;



            Target.GetComponent<Interactable>()?.Interaction(_owner as Agent);
            return true;

        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + (Vector3)boxOffset, boxSize);

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(
                transform.position + (Vector3)groundCheckOffset,
                groundCheckSize);
        }
    }
}