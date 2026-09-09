using System;
using System.Collections;
using KimLIb.AnimatorSystems;
using Member.KYM.Scripts.CombatSystems.Projectiles;
using Member.KYM.Scripts.CombatSystems.WeaponSystems;
using UnityEngine;
using UnityEngine.Serialization;

namespace Member.KYM.Scripts.Enemies.Boss.BossSkills.TutoBoss
{
    [DisallowMultipleComponent]
    public class BossGun : AbstractWeapon
    {
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
        [SerializeField, Min(0f)] private float damageMultiplier = 1f;

        [Header("산탄")]
        [SerializeField, Min(1)] private int projectileCount = 1;
        [SerializeField, Range(0f, 360f)] private float spreadAngle;
        [SerializeField] private bool randomizeSpread;

        [Header("연발")]
        [SerializeField, Min(1)] private int burstCount = 1;
        [SerializeField, Min(0f)] private float burstInterval = 0.1f;
        [SerializeField, Min(0f)] private float recoveryTime = 0.1f;

        public event Action<GameObject> OnProjectileSpawned;

        private Coroutine _fireRoutine;
        private float _aimAngularVelocity;

        private void Awake()
        {
            if (gunHolder == null)
                gunHolder = transform;

            if (muzzle == null)
                muzzle = transform;

            if (animator == null)
                animator = GetComponent<Animator>();
        }

        public override bool CanAttack(GameObject target = null)
        {
            return base.CanAttack(target) &&
                   target != null &&
                   gunHolder != null &&
                   abstractProjectilePrefab != null;
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
            GameObject attackOwner = Owner != null ? Owner.gameObject : gameObject;
            _fireRoutine = StartCoroutine(FireSequence(direction, attackOwner, damageMultiplier));
        }

        public override void StopAttack()
        {
            if (!IsAttacking)
                return;

            if (_fireRoutine != null)
                StopCoroutine(_fireRoutine);

            _fireRoutine = null;
            base.StopAttack();
        }

        public override void Unequip()
        {
            base.Unequip();
            _aimAngularVelocity = 0f;
        }

        private IEnumerator FireSequence(Vector2 direction, GameObject owner, float damageMultiplier)
        {
            int safeBurstCount = Mathf.Max(1, burstCount);

            for (int burstIndex = 0; burstIndex < safeBurstCount; burstIndex++)
            {
                PlayFireAnimation();
                Vector2 currentDirection = muzzle != null
                    ? (Vector2)muzzle.right
                    : direction;
                FireProjectiles(currentDirection.normalized, owner, damageMultiplier);

                if (burstIndex < safeBurstCount - 1 && burstInterval > 0f)
                    yield return new WaitForSeconds(burstInterval);
            }

            if (recoveryTime > 0f)
                yield return new WaitForSeconds(recoveryTime);

            CompleteFireSequence();
        }

        private void FireProjectiles(Vector2 direction, GameObject owner, float damageMultiplier)
        {
            int safeProjectileCount = Mathf.Max(1, projectileCount);
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
                SpawnProjectile(shotDirection.normalized, owner, damageMultiplier);
            }
        }

        private void SpawnProjectile(Vector2 direction, GameObject owner, float damageMultiplier)
        {
            float rotationZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            AbstractProjectile abstractProjectileObject = Instantiate(
                abstractProjectilePrefab,
                muzzle.position,
                Quaternion.Euler(0f, 0f, rotationZ));

            abstractProjectileObject.Shot(
                direction,
                owner,
                projectileSpeed,
                damageMultiplier);

            OnProjectileSpawned?.Invoke(abstractProjectileObject.gameObject);
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
        }

        private void Update()
        {
            if (trackTargetWhileEquipped)
                TryAimAtTarget();
        }

        private bool TryAimAtTarget()
        {
            if (Target == null || gunHolder == null)
                return false;

            Vector2 direction = Target.transform.position - gunHolder.position;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return false;

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
