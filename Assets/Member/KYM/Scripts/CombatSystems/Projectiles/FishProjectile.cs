using KimLIb.ModuleSystems;
using UnityEngine;
using UnityEngine.Events;

namespace Member.KYM.Scripts.CombatSystems.Projectiles
{
    [DisallowMultipleComponent]
    public sealed class FishProjectile : GrabbableProjectile
    {
        [Header("물고기 이동")]
        [SerializeField, Min(0.01f)] private float flightGravityScale = 1f;
        [SerializeField] private LayerMask floorLayers = 1 << 3;
        [SerializeField] private LayerMask waterLayers = 1 << 4;
        [SerializeField, Min(0)] private int maxFloorBounces = 2;
        [SerializeField, Range(0f, 2f)] private float bounceHeightMultiplier = 0.7f;
        [SerializeField, Range(0f, 1f)] private float bounceHorizontalMultiplier = 0.9f;
        [SerializeField, Min(0f)] private float floorSeparation = 0.03f;

        [Header("반사 이벤트")]
        [SerializeField] private UnityEvent onFloorBounce;

        public int FloorBounceCount { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Rigidbody.gravityScale = flightGravityScale;
        }

        public override void Shot(Vector2 direction, ModuleOwner owner, float launchSpeed)
        {
            FloorBounceCount = 0;
            base.Shot(direction, owner, launchSpeed);
        }

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            if (IsHeld || other == null)
                return;

            if (IsInLayer(other, waterLayers))
            {
                // 물속에서 발사할 때는 통과하고, 떨어져 돌아왔을 때만 퇴장한다.
                if (Rigidbody.linearVelocity.y < 0f)
                    Destroy(gameObject);
                return;
            }

            if (IsInLayer(other, floorLayers))
            {
                // 물 아래에서 솟아오를 때 바닥을 통과할 수 있어야 한다.
                if (Rigidbody.linearVelocity.y >= 0f)
                    return;

                if (TryBounceOnFloor(other))
                    return;
            }

            base.OnTriggerEnter2D(other);
        }

        private bool TryBounceOnFloor(Collider2D floor)
        {
            if (FloorBounceCount >= maxFloorBounces)
                return false;

            GetImpactContact(floor, out Vector2 hitPoint, out Vector2 hitNormal);
            if (hitNormal.y < 0.5f)
                return false;

            Vector2 incomingVelocity = Rigidbody.linearVelocity;
            float reboundSpeed = -incomingVelocity.y * bounceHeightMultiplier;
            if (reboundSpeed <= Mathf.Epsilon)
                return false;

            FloorBounceCount++;

            // 접점 바로 위로 빼서 트리거 안에 남아 다음 낙하를 놓치지 않게 한다.
            Bounds bounds = ProjectileCollider.bounds;
            float colliderCenterOffsetY = bounds.center.y - Rigidbody.position.y;
            float bodyY = hitPoint.y + bounds.extents.y - colliderCenterOffsetY + floorSeparation;
            Rigidbody.position = new Vector2(Rigidbody.position.x, bodyY);
            Rigidbody.linearVelocity = new Vector2(
                incomingVelocity.x * bounceHorizontalMultiplier,
                reboundSpeed);

            onFloorBounce?.Invoke();
            return true;
        }

        private static bool IsInLayer(Collider2D collider, LayerMask mask)
        {
            return (mask.value & (1 << collider.gameObject.layer)) != 0;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            flightGravityScale = Mathf.Max(0.01f, flightGravityScale);
            maxFloorBounces = Mathf.Max(0, maxFloorBounces);
            floorSeparation = Mathf.Max(0f, floorSeparation);
        }
    }
}
