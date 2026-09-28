using KimLIb.ModuleSystems;
using Member.KYM.Scripts.CombatSystems.Projectiles;
using UnityEngine;
using UnityEngine.Events;

namespace Member.KYM.Scripts.CoreSystems.TutorialSystem
{
    [DisallowMultipleComponent]
    public sealed class TutorialTurret : ModuleOwner
    {
        [Header("고정 발사 방향")]
        [SerializeField] private Transform muzzle;
        [SerializeField] private Vector2 localFireDirection = Vector2.right;
        [SerializeField] private AbstractProjectile projectilePrefab;

        [Header("반복 발사")]
        [SerializeField] private bool fireOnEnable;
        [SerializeField, Min(0f)] private float firstShotDelay = 0.5f;
        [SerializeField, Min(0.01f)] private float fireInterval = 2f;

        [Header("발사 이벤트")]
        [SerializeField] private UnityEvent onFired;

        private bool _isFiring;
        private float _nextShotTime;

        protected override void Awake()
        {
            base.Awake();

            if (muzzle == null)
                muzzle = transform;

            Debug.Assert(projectilePrefab != null, "튜토리얼 터렛에 투사체 프리팹이 없습니다.", this);
        }

        private void OnEnable()
        {
            if (fireOnEnable)
                StartFiring();
        }

        private void OnDisable()
        {
            StopFiring();
        }

        private void Update()
        {
            if (!_isFiring || Time.time < _nextShotTime)
                return;

            FireOnce();
            _nextShotTime = Time.time + fireInterval;
        }

        public void StartFiring()
        {
            _isFiring = true;
            _nextShotTime = Time.time + firstShotDelay;
        }

        public void StopFiring()
        {
            _isFiring = false;
        }

        public void FireOnce()
        {
            if (projectilePrefab == null || muzzle == null || localFireDirection.sqrMagnitude <= Mathf.Epsilon)
                return;

            Vector2 direction = muzzle.TransformDirection(localFireDirection).normalized;
            AbstractProjectile projectile = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity);
            projectile.Shot(direction, this);
            onFired?.Invoke();
        }

        private void OnValidate()
        {
            firstShotDelay = Mathf.Max(0f, firstShotDelay);
            fireInterval = Mathf.Max(0.01f, fireInterval);
        }
    }
}
