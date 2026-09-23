using DG.Tweening;
using KimLIb.AnimatorSystems;
using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.CombatSystems.Projectiles;
using Member.KYM.Scripts.CoreSystems;
using UnityEngine;

namespace Member.KYM.Scripts.Enemies
{
    [DisallowMultipleComponent]
    public class Sun : Agent
    {
        [Header("타겟")]
        [field: SerializeField] public GameObject Target { get; private set; }

        [Header("부유 연출")]
        [Tooltip("루트의 물리 위치는 유지하고 이 Transform만 위아래로 움직입니다.")]
        [SerializeField] private Transform floatingVisual;
        [SerializeField, Min(0f)] private float floatingHeight = 0.25f;
        [SerializeField, Min(0.01f)] private float floatingDuration = 1f;
        [SerializeField] private Ease floatingEase = Ease.InOutSine;

        [Header("공격")]
        [SerializeField, Min(0.01f)] private float attackInterval = 7.5f;
        [SerializeField] private AnimParamSO attackAnimation;
        [SerializeField] private AnimatorTrigger animatorTrigger;
        [SerializeField] private Transform muzzle;
        [SerializeField] private AbstractProjectile projectilePrefab;

        private IAnimateRenderer _renderer;
        private Tween _floatingTween;
        private Vector3 _floatingStartLocalPosition;
        private float _nextAttackTime;
        private bool _isFiring;

        protected override void Awake()
        {
            base.Awake();

            _renderer = GetModule<IAnimateRenderer>();

            if (animatorTrigger == null)
                animatorTrigger = GetComponentInChildren<AnimatorTrigger>(true);

            if (floatingVisual == null)
                floatingVisual = transform;

            if (muzzle == null)
                muzzle = transform;

            _floatingStartLocalPosition = floatingVisual.localPosition;

            Debug.Assert(_renderer != null, $"{name}에 IAnimateRenderer 모듈이 없습니다.");
            Debug.Assert(animatorTrigger != null, $"{name}에 AnimatorTrigger가 없습니다.");
            Debug.Assert(projectilePrefab != null, $"{name}에 투사체 프리팹이 없습니다.");
        }

        private void OnEnable()
        {
            if (animatorTrigger != null)
                animatorTrigger.OnSpecialEvent += HandleFireAnimationEvent;

            StartFloating();
        }

        private void OnDisable()
        {
            if (animatorTrigger != null)
                animatorTrigger.OnSpecialEvent -= HandleFireAnimationEvent;

            _isFiring = false;
            StopFloating();
        }

        public void ActivateAndStartFiring()
        {
            gameObject.SetActive(true);
            _isFiring = true;
            
            _nextAttackTime = Time.time + attackInterval;
        }

        private void Update()
        {
            FaceTarget();

            if (!_isFiring || Target == null || Time.time < _nextAttackTime)
                return;

            BeginAttack();
        }

        private void FaceTarget()
        {
            if (Target == null)
                return;

            float targetX = Target.transform.position.x;
            if (Mathf.Approximately(targetX, transform.position.x))
                return;

            Vector3 rotation = transform.localEulerAngles;
            rotation.y = targetX > transform.position.x ? 180f : 0f;
            transform.localEulerAngles = rotation;
        }

        private void BeginAttack()
        {
            if (_renderer?.Animator == null || attackAnimation == null)
                return;

            _nextAttackTime = Time.time + attackInterval;
            _renderer.Animator.Play(attackAnimation.ParamHash, 0, 0f);
        }

        private void HandleFireAnimationEvent()
        {
            if (!_isFiring)
                return;

            _nextAttackTime = Time.time + attackInterval;

            if (Target == null || projectilePrefab == null || muzzle == null)
                return;

            Vector2 direction = Target.transform.position - muzzle.position;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                direction = Vector2.down;

            AbstractProjectile projectile = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity);
            projectile.Shot(direction.normalized, this);
        }

        private void StartFloating()
        {
            if (floatingVisual == null)
                return;

            StopFloating();
            floatingVisual.localPosition = _floatingStartLocalPosition;

            _floatingTween = floatingVisual
                .DOLocalMoveY(
                    _floatingStartLocalPosition.y + floatingHeight,
                    floatingDuration)
                .SetEase(floatingEase)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void StopFloating()
        {
            _floatingTween?.Kill();
            _floatingTween = null;

            if (floatingVisual != null)
                floatingVisual.localPosition = _floatingStartLocalPosition;
        }

        private void OnValidate()
        {
            floatingHeight = Mathf.Max(0f, floatingHeight);
            floatingDuration = Mathf.Max(0.01f, floatingDuration);
            attackInterval = Mathf.Max(0.01f, attackInterval);
        }
    }
}
