using UnityEngine;

namespace Member.KYM.Scripts.RobotArm
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class GrabbableRigidbody2D : MonoBehaviour, IGrabbable
    {
        [SerializeField] private bool canBeGrabbed = true;
        [SerializeField] private bool alignRotationWhileHeld = true;

        public bool CanBeGrabbed => canBeGrabbed && !_isHeld;
        public Transform GrabTransform => transform;
        public bool IsHeld => _isHeld;

        protected Rigidbody2D Body { get; private set; }
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
            Body = GetComponent<Rigidbody2D>();
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
            _originalBodyType = Body.bodyType;
            _originalConstraints = Body.constraints;
            _originalGravityScale = Body.gravityScale;

            Body.linearVelocity = Vector2.zero;
            Body.angularVelocity = 0f;
            Body.bodyType = RigidbodyType2D.Kinematic;
            Body.gravityScale = 0f;
            Body.constraints = RigidbodyConstraints2D.FreezeRotation;

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

        public virtual void Throw(Vector2 velocity, GameObject newOwner)
        {
            if (!_isHeld)
                return;

            RestorePhysicsState();
            Body.linearVelocity = velocity;
            OnThrown(newOwner, velocity);
        }

        protected virtual void OnGrabbed() { }
        protected virtual void OnReleased() { }
        protected virtual void OnThrown(GameObject newOwner, Vector2 velocity) { }

        private void RestorePhysicsState()
        {
            transform.SetParent(_originalParent, true);

            Body.bodyType = _originalBodyType;
            Body.constraints = _originalConstraints;
            Body.gravityScale = _originalGravityScale;

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
