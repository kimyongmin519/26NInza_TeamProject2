using System.Collections;
using Member.KYM.Scripts.CombatSystems.DamageSystems;
using Member.KYM.Scripts.Players.RobotArm;
using Member.ODK._01_Script;
using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.Projectiles
{
    public class GrabbableProjectile : AbstractProjectile, IGrabbable
    {
        [Header("피해")]
        [SerializeField, Min(0f)] private float damage = 1f;
        [SerializeField, Min(0f)] private float knockbackForce;
        [SerializeField, Min(0f)] private float reflectedDamageMultiplier = 1f;

        [Header("수명")]
        [SerializeField, Min(0f)] private float lifetime = 5f;
        [SerializeField] private bool destroyOnNonDamageableImpact = true;

        [Header("잡기")]
        [SerializeField] private bool canBeGrabbed = true;
        [SerializeField] private bool alignRotationWhileHeld = true;

        public bool CanBeGrabbed => canBeGrabbed && !_isHeld && !_hasImpacted;
        public Transform GrabTransform => transform;

        private Collider2D _collider;
        private bool _colliderEnabledState;
        private RigidbodyType2D _originalBodyType;
        private RigidbodyConstraints2D _originalConstraints;
        private float _originalGravityScale;
        private Transform _originalParent;
        private GameObject _grabber;
        private bool _isHeld;
        private bool _hasImpacted;
        private Coroutine _lifetimeRoutine;

        private AbstractDamageCaster _damageCaster;

        protected override void Awake()
        {
            base.Awake();

            _collider = GetComponent<Collider2D>();
            _damageCaster = GetComponentInChildren<AbstractDamageCaster>();
            Debug.Assert(_collider != null, "프로젝타일은 같은 오브젝트에 Collider2D가 있어야함!");
            Debug.Assert(_damageCaster != null, "프로젝타일은 데미지캐스터가 자식에 있어야함!");

            if (_collider != null)
                GrabbableLayer.TryApply(_collider.gameObject);
        }

        private void OnEnable()
        {
            _hasImpacted = false;
            RestartLifetime();
        }

        private void OnDisable()
        {
            StopLifetime();
        }

        public void Grab(Transform grabPoint, GameObject grabber)
        {
            if (!CanBeGrabbed || grabPoint == null)
                return;

            _isHeld = true;
            _grabber = grabber;
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

            GameObject releaseOwner = _grabber;
            RestorePhysicsState();
            Shot(Vector2.zero, releaseOwner, 0f, DamageMultiplier);
            RestartLifetime();
        }

        public void Throw(ThrowData throwData)
        {
            if (!_isHeld)
                return;

            float reflectedDamage = DamageMultiplier * reflectedDamageMultiplier;
            RestorePhysicsState();
            _hasImpacted = false;

            Shot(
                throwData.Direction,
                throwData.Owner,
                throwData.ArmThrowSpeed,
                reflectedDamage);

            RestartLifetime();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleImpact(other.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleImpact(collision.gameObject);
        }

        private void HandleImpact(GameObject hitObject)
        {
            if (_isHeld || _hasImpacted || hitObject == null || IsOwner(hitObject))
                return;

            IDamageable damageable = FindDamageable(hitObject);
            if (damageable == null && !destroyOnNonDamageableImpact)
                return;

            _hasImpacted = true;

            if (damageable != null)
            {
                Vector2 direction = Rigidbody.linearVelocity.sqrMagnitude > Mathf.Epsilon
                    ? Rigidbody.linearVelocity.normalized
                    : (Vector2)transform.right;

                damageable.TakeDamage(new DamageData(
                    damage * DamageMultiplier,
                    DamageType.Projectile,
                    CriticalType.Normal,
                    direction * knockbackForce));
            }

            Destroy(gameObject);
        }

        private bool IsOwner(GameObject hitObject)
        {
            if (Owner == null)
                return false;

            Transform hitTransform = hitObject.transform;
            Transform ownerTransform = Owner.transform;
            return hitTransform == ownerTransform || hitTransform.IsChildOf(ownerTransform);
        }

        private static IDamageable FindDamageable(GameObject hitObject)
        {
            MonoBehaviour[] behaviours = hitObject.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IDamageable damageable)
                    return damageable;
            }

            return null;
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

        private void OnValidate()
        {
            damage = Mathf.Max(0f, damage);
            knockbackForce = Mathf.Max(0f, knockbackForce);
            reflectedDamageMultiplier = Mathf.Max(0f, reflectedDamageMultiplier);
            lifetime = Mathf.Max(0f, lifetime);

            Collider2D projectileCollider = GetComponent<Collider2D>();
            if (projectileCollider != null)
                GrabbableLayer.TryApply(projectileCollider.gameObject);
        }
    }
}
