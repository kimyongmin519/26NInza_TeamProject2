using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using KimLIb.AnimatorSystems;
using KimLIb.ModuleSystems;
using Member.KYM.Scripts.CombatSystems.Projectiles;
using Member.KYM.Scripts.CombatSystems.WeaponSystems;
using UnityEngine;
using UnityEngine.Serialization;

namespace Member.KYM.Scripts.Enemies.Boss.BossSkills.TutoBoss
{
    [DisallowMultipleComponent]
    public class BossGun : AbstractWeapon
    {
        [Serializable]
        private class ThrowMotionStep
        {
            [Tooltip("연출 시작 위치를 기준으로 한 로컬 위치 오프셋")]
            public Vector3 localPositionOffset;

            [Tooltip("연출 시작 회전을 기준으로 한 로컬 회전 오프셋")]
            public Vector3 localRotationOffset;

            [Min(0f)] public float duration = 0.15f;
            public Ease ease = Ease.OutQuad;
        }

        [Header("참조")]
        [SerializeField] private Transform gunHolder;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Animator animator;
        [SerializeField] private AnimParamSO fireAnimation;
        [FormerlySerializedAs("trackTargetWhileAttacking")]
        [SerializeField] private bool trackTargetWhileEquipped = true;
        [SerializeField, Min(0f)] private float aimSmoothTime = 0.12f;

        [Header("탄환")]
        [FormerlySerializedAs("projectilePrefab")]
        [SerializeField] private AbstractProjectile abstractProjectilePrefab;
        [SerializeField, Min(0f)] private float projectileSpeed = 10f;

        [Header("산탄")]
        [SerializeField, Min(1)] private int projectileCount = 1;
        [SerializeField, Range(0f, 360f)] private float spreadAngle;
        [SerializeField] private bool randomizeSpread;

        [Header("연발")]
        [SerializeField, Min(1)] private int burstCount = 1;
        [SerializeField, Min(0f)] private float burstInterval = 0.1f;
        [SerializeField, Min(0f)] private float recoveryTime = 0.1f;

        [Header("사용 후 무기 투척 (꺼놓으면 직렬화 값 굳이 안넣어도 됨)")]
        [SerializeField] private bool throwWeaponAfterAttack;
        [SerializeField] private GrabbableProjectile thrownWeaponPrefab;
        [SerializeField] private Transform weaponThrowPoint;
        [SerializeField, Min(0f)] private float thrownWeaponSpeed = 12f;
        [SerializeField, Min(0)] private int weaponIndexAfterThrow;

        [Header("무기 투척 연출")]
        [SerializeField, Min(0f)] private float throwDelay = 0.25f;
        [SerializeField] private Transform throwMotionTarget;
        [SerializeField] private ThrowMotionStep[] throwMotionSteps;

        public event Action<GameObject> OnProjectileSpawned;

        private Coroutine _fireRoutine;
        private Sequence _throwMotionSequence;
        private float _aimAngularVelocity;
        private bool _isPlayingThrowMotion;
        private Vector3 _throwMotionStartLocalPosition;
        private Vector3 _throwMotionStartLocalEulerAngles;

        private void Awake()
        {
            if (gunHolder == null)
                gunHolder = transform;

            if (muzzle == null)
                muzzle = transform;

            if (animator == null)
                animator = GetComponent<Animator>();

            if (throwMotionTarget == null)
                throwMotionTarget = gunHolder;

            CacheThrowMotionStartPose();
        }

        public override void Equip()
        {
            base.Equip();
            RestoreThrowMotionTarget();
        }

        public override bool CanAttack(GameObject target = null)
        {
            return base.CanAttack(target) &&
                   target != null &&
                   gunHolder != null &&
                   abstractProjectilePrefab != null &&
                   (!throwWeaponAfterAttack || thrownWeaponPrefab != null);
        }

        public override void Attack(GameObject target = null)
        {
            if (!CanAttack(target))
                return;

            base.Attack(target);

            if (!TryAimAtTarget())
            {
                StopAttack();
                return;
            }

            Vector2 direction = gunHolder.right;
            ModuleOwner attackOwner = Owner;
            _fireRoutine = StartCoroutine(FireSequence(direction, attackOwner));
        }

        public override void StopAttack()
        {
            if (_fireRoutine != null)
                StopCoroutine(_fireRoutine);

            _fireRoutine = null;
            CancelThrowMotion();

            if (IsAttacking)
                base.StopAttack();
        }

        public override void Unequip()
        {
            base.Unequip();
            _aimAngularVelocity = 0f;
            RestoreThrowMotionTarget();
        }

        private IEnumerator FireSequence(Vector2 direction, ModuleOwner owner)
        {
            int safeBurstCount = Mathf.Max(1, burstCount);

            for (int burstIndex = 0; burstIndex < safeBurstCount; burstIndex++)
            {
                PlayFireAnimation();
                Vector2 currentDirection = muzzle != null
                    ? (Vector2)muzzle.right
                    : direction;
                FireProjectiles(currentDirection.normalized, owner);

                if (burstIndex < safeBurstCount - 1 && burstInterval > 0f)
                    yield return new WaitForSeconds(burstInterval);
            }

            if (throwWeaponAfterAttack)
            {
                if (throwDelay > 0f)
                    yield return new WaitForSeconds(throwDelay);

                yield return PlayThrowMotion();

                if (!IsAttacking)
                    yield break;

                ThrowWeapon();
                HideThrownWeaponVisual();
            }

            if (recoveryTime > 0f)
                yield return new WaitForSeconds(recoveryTime);

            CompleteFireSequence();
        }

        private IEnumerator PlayThrowMotion()
        {
            if (throwMotionTarget == null ||
                throwMotionSteps == null ||
                throwMotionSteps.Length == 0)
            {
                yield break;
            }

            CacheThrowMotionStartPose();
            _isPlayingThrowMotion = true;
            _throwMotionSequence = DOTween.Sequence();
            float facingDirection = GetThrowMotionFacingDirection();

            foreach (ThrowMotionStep step in throwMotionSteps)
            {
                if (step == null)
                    continue;

                float duration = Mathf.Max(0f, step.duration);
                Vector3 positionOffset = step.localPositionOffset;
                Vector3 rotationOffset = step.localRotationOffset;
                positionOffset.x *= facingDirection;
                rotationOffset.z *= facingDirection;

                Vector3 targetPosition =
                    _throwMotionStartLocalPosition + positionOffset;
                Vector3 targetRotation =
                    _throwMotionStartLocalEulerAngles + rotationOffset;

                _throwMotionSequence.Append(
                    throwMotionTarget
                        .DOLocalMove(targetPosition, duration)
                        .SetEase(step.ease));
                _throwMotionSequence.Join(
                    throwMotionTarget
                        .DOLocalRotate(
                            targetRotation,
                            duration,
                            RotateMode.FastBeyond360)
                        .SetEase(step.ease));
            }

            yield return _throwMotionSequence.WaitForCompletion();

            _throwMotionSequence = null;
            _isPlayingThrowMotion = false;
        }

        private void FireProjectiles(Vector2 direction, ModuleOwner owner)
        {
            int safeProjectileCount = Mathf.Max(1, projectileCount);
            List<Collider2D> spawnedColliders = new(safeProjectileCount);
            float angleStep = safeProjectileCount > 1
                ? spreadAngle / (safeProjectileCount - 1)
                : 0f;
            float startAngle = safeProjectileCount > 1 ? -spreadAngle * 0.5f : 0f;

            for (int projectileIndex = 0; projectileIndex < safeProjectileCount; projectileIndex++)
            {
                float angleOffset = randomizeSpread
                    ? UnityEngine.Random.Range(-spreadAngle * 0.5f, spreadAngle * 0.5f)
                    : startAngle + angleStep * projectileIndex;

                Vector2 shotDirection = Quaternion.Euler(0f, 0f, angleOffset) * direction;
                Collider2D spawnedCollider = SpawnProjectile(shotDirection.normalized, owner);
                if (spawnedCollider == null)
                    continue;

                foreach (Collider2D otherCollider in spawnedColliders)
                    Physics2D.IgnoreCollision(spawnedCollider, otherCollider);

                spawnedColliders.Add(spawnedCollider);
            }
        }

        private Collider2D SpawnProjectile(Vector2 direction, ModuleOwner owner)
        {
            float rotationZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            AbstractProjectile abstractProjectileObject = Instantiate(
                abstractProjectilePrefab,
                muzzle.position,
                Quaternion.Euler(0f, 0f, rotationZ));

            abstractProjectileObject.Shot(
                direction,
                owner,
                projectileSpeed);

            OnProjectileSpawned?.Invoke(abstractProjectileObject.gameObject);
            return abstractProjectileObject.GetComponent<Collider2D>();
        }

        private void ThrowWeapon()
        {
            Transform throwPoint = weaponThrowPoint != null
                ? weaponThrowPoint
                : muzzle;
            if (thrownWeaponPrefab == null || throwPoint == null)
                return;

            Vector2 direction = Target != null
                ? Target.transform.position - throwPoint.position
                : gunHolder.right;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                direction = gunHolder.right;

            GrabbableProjectile thrownWeapon = Instantiate(
                thrownWeaponPrefab,
                throwPoint.position,
                Quaternion.identity);

            thrownWeapon.Shot(
                direction.normalized,
                Owner,
                thrownWeaponSpeed);

            OnProjectileSpawned?.Invoke(thrownWeapon.gameObject);
        }

        private void HideThrownWeaponVisual()
        {
            if (throwMotionTarget != null &&
                throwMotionTarget != transform)
            {
                throwMotionTarget.gameObject.SetActive(false);
            }
        }

        private void CancelThrowMotion()
        {
            _throwMotionSequence?.Kill();
            _throwMotionSequence = null;
            _isPlayingThrowMotion = false;
            RestoreThrowMotionTarget();
        }

        private void CacheThrowMotionStartPose()
        {
            if (throwMotionTarget == null)
                return;

            _throwMotionStartLocalPosition = throwMotionTarget.localPosition;
            _throwMotionStartLocalEulerAngles = throwMotionTarget.localEulerAngles;
        }

        private void RestoreThrowMotionTarget()
        {
            if (throwMotionTarget == null)
                return;

            throwMotionTarget.localPosition = _throwMotionStartLocalPosition;
            throwMotionTarget.localEulerAngles = _throwMotionStartLocalEulerAngles;

            if (throwMotionTarget != transform)
                throwMotionTarget.gameObject.SetActive(true);
        }

        private void PlayFireAnimation()
        {
            if (animator != null && fireAnimation != null)
                animator.Play(fireAnimation.ParamHash, 0, 0f);
        }

        private void CompleteFireSequence()
        {
            _fireRoutine = null;
            base.StopAttack();

            if (throwWeaponAfterAttack)
                WeaponModule?.TryEquipWeapon(weaponIndexAfterThrow);
        }

        private void Update()
        {
            if (trackTargetWhileEquipped && !_isPlayingThrowMotion)
                TryAimAtTarget();
        }

        private float GetThrowMotionFacingDirection()
        {
            if (Target != null)
            {
                float directionX = Target.transform.position.x - gunHolder.position.x;
                if (!Mathf.Approximately(directionX, 0f))
                    return Mathf.Sign(directionX);
            }

            return gunHolder.right.x < 0f ? -1f : 1f;
        }

        private bool TryAimAtTarget()
        {
            if (Target == null || gunHolder == null)
                return false;

            Vector2 direction = Target.transform.position - gunHolder.position;

            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float angle = aimSmoothTime > 0f
                ? Mathf.SmoothDampAngle(
                    gunHolder.eulerAngles.z,
                    targetAngle,
                    ref _aimAngularVelocity,
                    aimSmoothTime)
                : targetAngle;

            gunHolder.rotation = Quaternion.Euler(0f, 0f, angle);

            Vector3 holderScale = gunHolder.localScale;
            float scaleY = Mathf.Abs(holderScale.y);
            float signedAngle = Mathf.DeltaAngle(0f, angle);
            bool isAimingBackward = signedAngle > 90f || signedAngle < -90f;
            holderScale.y = isAimingBackward ? -scaleY : scaleY;
            gunHolder.localScale = holderScale;

            return true;
        }

        private void OnDisable()
        {
            StopAttack();
        }
    }
}
