using UnityEngine;

namespace Member.KYM.Scripts.Players.RobotArm
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class GrabbableRigidbody2D : MonoBehaviour, IGrabbable
    {
        [SerializeField] private bool canBeGrabbed = true;
        [SerializeField] private bool alignRotationWhileHeld = true;

        public bool CanBeGrabbed => canBeGrabbed && !_isHeld;
        public Transform GrabTransform => transform;
        public bool IsHeld => _isHeld;

        protected Rigidbody2D Rigidbody { get; private set; }
        protected GameObject Grabber { get; private set; }

        private Collider2D[] _colliders;
        private bool[] _colliderEnabledStates;
        private RigidbodyType2D _originalBodyType;
        private RigidbodyConstraints2D _originalConstraints;
        private float _originalGravityScale;
        private Transform _originalParent;
        private bool _isHeld;

        protected virtual void Awake()
        {
            Rigidbody = GetComponent<Rigidbody2D>();
            _colliders = GetComponentsInChildren<Collider2D>(true);
            _colliderEnabledStates = new bool[_colliders.Length];
        }

        public virtual void Grab(Transform grabPoint, GameObject grabber)
        {
            if (!CanBeGrabbed || grabPoint == null)
                return;

            _isHeld = true;
            Grabber = grabber;
            _originalParent = transform.parent;
            _originalBodyType = Rigidbody.bodyType;
            _originalConstraints = Rigidbody.constraints;
            _originalGravityScale = Rigidbody.gravityScale;

            Rigidbody.linearVelocity = Vector2.zero;
            Rigidbody.angularVelocity = 0f;
            Rigidbody.bodyType = RigidbodyType2D.Kinematic;
            Rigidbody.gravityScale = 0f;
            Rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;

            SetCollidersEnabled(false);

            transform.SetParent(grabPoint, true);
            transform.localPosition = Vector3.zero;
            if (alignRotationWhileHeld)
                transform.localRotation = Quaternion.identity;

            OnGrabbed();
        }

        public virtual void Release()
        {
            if (!_isHeld)
                return;

            RestorePhysicsState();
            OnReleased();
        }

        public virtual void Throw(ThrowData throwData)
        {
            if (!_isHeld)
                return;

            RestorePhysicsState();
            Rigidbody.linearVelocity = throwData.Direction * throwData.ArmThrowSpeed;

            OnThrown(throwData);
        }

        protected virtual void OnGrabbed() { }
        protected virtual void OnReleased() { }
        protected virtual void OnThrown(ThrowData throwData) { }

        private void RestorePhysicsState()
        {
            transform.SetParent(_originalParent, true);

            Rigidbody.bodyType = _originalBodyType;
            Rigidbody.constraints = _originalConstraints;
            Rigidbody.gravityScale = _originalGravityScale;

            SetCollidersEnabled(true);
            _isHeld = false;
            Grabber = null;
        }

        private void SetCollidersEnabled(bool restore)
        {
            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] == null)
                    continue;

                if (!restore)
                {
                    _colliderEnabledStates[i] = _colliders[i].enabled;
                    _colliders[i].enabled = false;
                }
                else
                {
                    _colliders[i].enabled = _colliderEnabledStates[i];
                }
            }
        }
    }
}
