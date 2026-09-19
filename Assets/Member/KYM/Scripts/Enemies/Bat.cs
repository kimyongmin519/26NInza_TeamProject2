using Member.KYM.Scripts.Agents;
using UnityEngine;

namespace Member.KYM.Scripts.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class Bat : Agent
    {
        [Header("이동")]
        [SerializeField, Min(0.01f)] private float moveInterval = 0.5f;
        [SerializeField, Min(0f)] private float moveDistance = 0.5f;

        [field:SerializeField] public GameObject Target { get; private set; }

        private Rigidbody2D _rigidBody;
        private float _moveTimer;

        protected override void Awake()
        {
            base.Awake();
            _rigidBody = GetComponent<Rigidbody2D>();
            _rigidBody.gravityScale = 0f;
        }

        public void SetTarget(GameObject target)
        {
            Target = target;
            _moveTimer = 0f;
        }

        private void FixedUpdate()
        {
            if (Target == null)
                return;

            _moveTimer += Time.fixedDeltaTime;
            if (_moveTimer < moveInterval)
                return;

            _moveTimer = 0f;
            MoveTowardTarget();
        }

        private void MoveTowardTarget()
        {
            Vector2 targetPosition = Target.transform.position;
            Vector2 nextPosition = Vector2.MoveTowards(
                _rigidBody.position,
                targetPosition,
                moveDistance);

            _rigidBody.MovePosition(nextPosition);
        }

        private void OnValidate()
        {
            moveInterval = Mathf.Max(0.01f, moveInterval);
            moveDistance = Mathf.Max(0f, moveDistance);
        }
    }
}
