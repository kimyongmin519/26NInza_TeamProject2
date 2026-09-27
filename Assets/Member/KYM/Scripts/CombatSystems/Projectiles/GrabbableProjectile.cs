using System.Collections;
using GGMLib.ObjectPool.Runtime;
using KimLIb.EventSystem;
using KimLIb.ModuleSystems;
using Member.KYM.Scripts.CombatSystems.DamageSystems;
using Member.KYM.Scripts.CoreSystems.Events;
using Member.KYM.Scripts.EffectSystems;
using Member.KYM.Scripts.Players.RobotArm;
using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.Projectiles
{
    public class GrabbableProjectile : AbstractProjectile, IGrabbable
    {
        [Header("수명")]
        [SerializeField, Min(0f)] private float lifetime = 5f;
        [SerializeField, Min(0f)] private float launchCollisionDelay;

        [Header("잡기")]
        [SerializeField] private bool canBeGrabbed = true;
        [SerializeField] private bool alignRotationWhileHeld = true;

        [Header("충돌 이펙트")]
        [SerializeField] private EventChannelSO createChannel;
        
        public bool CanBeGrabbed => canBeGrabbed && !_isHeld && !_hasImpacted;
        public Transform GrabTransform => transform;

        protected bool IsHeld => _isHeld;
        protected Collider2D ProjectileCollider => _collider;

        private Collider2D _collider;
        private bool _colliderEnabledState;
        private RigidbodyType2D _originalBodyType;
        private RigidbodyConstraints2D _originalConstraints;
        private float _originalGravityScale;
        private Transform _originalParent;
        private ModuleOwner _grabber;
        private bool _isHeld;
        private bool _hasImpacted;
        private Coroutine _lifetimeRoutine;
        private Coroutine _launchCollisionRoutine;

        private AbstractDamageCaster _damageCaster;

        protected override void Awake()
        {
            base.Awake();

            _collider = GetComponent<Collider2D>();
            _damageCaster = GetComponentInChildren<AbstractDamageCaster>();
            Debug.Assert(_collider != null, "프로젝타일은 같은 오브젝트에 Collider2D가 있어야함!");
            Debug.Assert(_damageCaster != null, "프로젝타일은 데미지캐스터가 자식에 있어야함!");

            if (_collider != null)
            {
                _collider.isTrigger = true;
                GrabbableLayer.Validate(_collider.gameObject);
            }
        }

        private void OnEnable()
        {
            _hasImpacted = false;
            RestartLifetime();
        }

        private void OnDisable()
        {
            StopLifetime();
            StopLaunchCollisionDelay(false);
        }

        public override void Shot(Vector2 direction, ModuleOwner owner, float launchSpeed)
        {
            base.Shot(direction, owner, launchSpeed);
            RestartLaunchCollisionDelay();
        }

        public void Grab(Transform grabPoint, GameObject grabber)
        {
            if (!CanBeGrabbed || grabPoint == null)
                return;

            StopLaunchCollisionDelay(true);
            _isHeld = true;
            _grabber = grabber != null
                ? grabber.GetComponentInParent<ModuleOwner>()
                : null;
            StopLifetime();

            _originalParent = transform.parent;
            _originalBodyType = Rigidbody.bodyType;
            _originalConstraints = Rigidbody.constraints;
            _originalGravityScale = Rigidbody.gravityScale;

            Rigidbody.linearVelocity = Vector2.zero;
            Rigidbody.angularVelocity = 0f;
            Rigidbody.bodyType = RigidbodyType2D.Kinematic;
            Rigidbody.gravityScale = 0f;
            Rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;

            SetColliderEnabled(false);
            transform.SetParent(grabPoint, true);
            transform.localPosition = Vector3.zero;

            if (alignRotationWhileHeld)
                transform.localRotation = Quaternion.identity;
        }

        public void Release()
        {
            if (!_isHeld)
                return;

            ModuleOwner releaseOwner = _grabber;
            RestorePhysicsState();
            Shot(Vector2.zero, releaseOwner, 0f);
            RestartLifetime();
        }

        public void Throw(ThrowData throwData)
        {
            if (!_isHeld)
                return;

            bool isEnemyProjectile = Owner != null && Owner != throwData.Owner;

            RestorePhysicsState();
            _hasImpacted = false;

            float throwSpeed = isEnemyProjectile ? ProjectileData.MoveSpeed * 2f : throwData.ArmThrowSpeed; //적의 투사체를 잡고 내가 역으로 던진거면 원래 속도보다 2배로 더 쎄게 날림

            Shot(
                throwData.Direction,
                throwData.Owner,
                throwSpeed);

            RestartLifetime();
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            HandleImpact(other);
        }

        private void HandleImpact(Collider2D hitCollider)
        {
            if (_isHeld ||
                _hasImpacted ||
                hitCollider == null ||
                IsOwner(hitCollider.gameObject))
            {
                return;
            }

            _hasImpacted = true;
            GetImpactContact(hitCollider, out Vector2 hitPoint, out Vector2 hitNormal);

            _damageCaster.InitCaster(Owner);
            _damageCaster.CastDamage(hitCollider, hitPoint, hitNormal);
            
            PlayImpactEffect(hitPoint, hitNormal);

            Destroy(gameObject);
        }

        protected void GetImpactContact(
            Collider2D hitCollider,
            out Vector2 hitPoint,
            out Vector2 hitNormal)
        {
            Vector2 travelDirection = Rigidbody.linearVelocity.sqrMagnitude > Mathf.Epsilon
                ? Rigidbody.linearVelocity.normalized
                : (Vector2)transform.right;

            hitPoint = hitCollider.ClosestPoint(Rigidbody.position);
            hitNormal = -travelDirection;

            if (_collider == null)
                return;

            ColliderDistance2D distance = _collider.Distance(hitCollider);
            if (!distance.isValid)
                return;

            hitPoint = distance.pointB;
            if (distance.normal.sqrMagnitude > Mathf.Epsilon)
                hitNormal = -distance.normal.normalized;
        }

        protected void PlayImpactEffect(Vector2 hitPoint, Vector2 hitNormal)
        {
            if (ProjectileData == null)
                return;

            PlayEffect(
                ProjectileData.ImpactItem,
                hitPoint,
                hitNormal,
                ProjectileData.ImpactColor);
        }

        protected void PlayEffect(
            PoolItemSO item,
            Vector2 hitPoint,
            Vector2 hitNormal,
            Color tint)
        {
            if (createChannel == null || item == null)
                return;

            Vector2 normal = hitNormal.sqrMagnitude > Mathf.Epsilon
                ? hitNormal.normalized
                : -(Vector2)transform.right;
            float rotationZ = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg;

            VfxSpawnContext context = new VfxSpawnContext(
                hitPoint,
                Quaternion.Euler(0f, 0f, rotationZ),
                tint);

            createChannel.RaiseEvent(CreateEvents.ShowPoolingEffect.InitData(item, context));
        }

        private bool IsOwner(GameObject hitObject)
        {
            if (Owner == null)
                return false;

            Transform hitTransform = hitObject.transform;
            Transform ownerTransform = Owner.transform;
            return hitTransform == ownerTransform || hitTransform.IsChildOf(ownerTransform);
        }

        private void RestorePhysicsState()
        {
            transform.SetParent(_originalParent, true);

            Rigidbody.bodyType = _originalBodyType;
            Rigidbody.constraints = _originalConstraints;
            Rigidbody.gravityScale = _originalGravityScale;
            Rigidbody.linearVelocity = Vector2.zero;
            Rigidbody.angularVelocity = 0f;

            SetColliderEnabled(true);
            _isHeld = false;
            _grabber = null;
        }

        private void SetColliderEnabled(bool restore)
        {
            if (_collider == null)
                return;

            if (!restore)
            {
                _colliderEnabledState = _collider.enabled;
                _collider.enabled = false;
                return;
            }

            _collider.enabled = _colliderEnabledState;
        }

        private void RestartLifetime()
        {
            StopLifetime();

            if (lifetime > 0f && isActiveAndEnabled && !_isHeld)
                _lifetimeRoutine = StartCoroutine(LifetimeRoutine());
        }

        private void StopLifetime()
        {
            if (_lifetimeRoutine == null)
                return;

            StopCoroutine(_lifetimeRoutine);
            _lifetimeRoutine = null;
        }

        private IEnumerator LifetimeRoutine()
        {
            yield return new WaitForSeconds(lifetime);
            _lifetimeRoutine = null;
            Destroy(gameObject);
        }

        private void RestartLaunchCollisionDelay()
        {
            StopLaunchCollisionDelay(true);

            if (_collider == null || launchCollisionDelay <= 0f)
                return;

            _collider.enabled = false;
            _launchCollisionRoutine = StartCoroutine(LaunchCollisionDelayRoutine());
        }

        private IEnumerator LaunchCollisionDelayRoutine()
        {
            yield return new WaitForSeconds(launchCollisionDelay);
            _launchCollisionRoutine = null;

            if (_collider != null && !_isHeld)
                _collider.enabled = true;
        }

        private void StopLaunchCollisionDelay(bool restoreCollider)
        {
            if (_launchCollisionRoutine != null)
            {
                StopCoroutine(_launchCollisionRoutine);
                _launchCollisionRoutine = null;
            }

            if (restoreCollider && _collider != null && !_isHeld)
                _collider.enabled = true;
        }

        protected virtual void OnValidate()
        {
            lifetime = Mathf.Max(0f, lifetime);
            launchCollisionDelay = Mathf.Max(0f, launchCollisionDelay);

            Collider2D projectileCollider = GetComponent<Collider2D>();
            if (projectileCollider != null)
            {
                projectileCollider.isTrigger = true;
            }
        }
    }
}
