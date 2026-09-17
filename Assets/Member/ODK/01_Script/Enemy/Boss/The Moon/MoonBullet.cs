using DG.Tweening;
using GGMLib.ObjectPool.Runtime;
using KimLIb.ObjectPool.Runtime;
using Member.KYM.Scripts.Players.RobotArm;
using Member.ODK.Scripts.Enemys.Combat;
using System;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class MoonBullet : GrabbableRigidbody, IPoolable
    {
        [Header("Optional Pool")]
        [SerializeField] private PoolManagerSO poolManagerSO;
        [SerializeField] private bool returnToPool;
        [field: SerializeField] public PoolItemSO PoolItem { get; set; }

        [Header("Visual")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private MoonTelegraphLine telegraphLine;

        public GameObject GameObject => gameObject;
        public LineRenderer ShowLineRenderer => lineRenderer;
        public event Action<Vector3> OnBossImpact;

        private Transform impactTarget;
        private Vector2 flightDirection;
        private float speed;
        private float acceleration;
        private float maximumSpeed;
        private float turnSpeed;
        private float playerDamage;
        private float bossDamage;
        private float lifeRemaining;
        private float impactDistance;
        private bool isFlying;
        private bool isThrown;
        private bool isConsumed;
        private Sequence warningSequence;

        protected override void Awake()
        {
            base.Awake();
            if (lineRenderer == null) lineRenderer = GetComponentInChildren<LineRenderer>(true);
            if (lineRenderer != null)
            {
                telegraphLine = lineRenderer.GetComponent<MoonTelegraphLine>();
                if (telegraphLine == null)
                    telegraphLine = lineRenderer.gameObject.AddComponent<MoonTelegraphLine>();
            }
        }

        public void Launch(
            Transform target,
            float warningDuration,
            float moveSpeed,
            float damageToPlayer,
            float damageToBoss,
            float lifeTime,
            float targetImpactDistance = 0.6f,
            float pullAcceleration = 0f,
            float maxSpeed = 0f,
            float pullTurnSpeed = 0f)
        {
            ResetState();
            impactTarget = target;
            speed = Mathf.Max(0.1f, moveSpeed);
            acceleration = Mathf.Max(0f, pullAcceleration);
            maximumSpeed = Mathf.Max(speed, maxSpeed <= 0f ? speed : maxSpeed);
            turnSpeed = Mathf.Max(0f, pullTurnSpeed);
            playerDamage = Mathf.Max(0f, damageToPlayer);
            bossDamage = Mathf.Max(0f, damageToBoss);
            lifeRemaining = Mathf.Max(0.1f, lifeTime);
            impactDistance = Mathf.Max(0.05f, targetImpactDistance);

            Vector3 targetPosition = impactTarget != null ? impactTarget.position : transform.position;
            flightDirection = ((Vector2)targetPosition - (Vector2)transform.position).normalized;
            DrawWarning(targetPosition, warningDuration);

            Rigidbody.bodyType = RigidbodyType2D.Kinematic;
            Rigidbody.linearVelocity = Vector2.zero;
            warningSequence = DOTween.Sequence();
            warningSequence.AppendInterval(Mathf.Max(0f, warningDuration));
            warningSequence.AppendCallback(BeginFlight);
        }

        public void ResetItem()
        {
            ResetState();
            gameObject.SetActive(true);
        }

        private void ResetState()
        {
            warningSequence?.Kill();
            warningSequence = null;
            isFlying = false;
            isThrown = false;
            isConsumed = false;
            impactTarget = null;
            if (Rigidbody != null)
            {
                Rigidbody.linearVelocity = Vector2.zero;
                Rigidbody.angularVelocity = 0f;
            }
            SetWarningVisible(false);
        }

        private void DrawWarning(Vector3 targetPosition, float warningDuration)
        {
            if (lineRenderer == null) return;
            if (telegraphLine != null)
                telegraphLine.Show(transform.position, targetPosition, warningDuration * 0.7f);
            else
            {
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, transform.position);
                lineRenderer.SetPosition(1, targetPosition);
                lineRenderer.enabled = true;
            }
        }
        private void BeginFlight()
        {
            if (isConsumed || IsHeld) return;
            SetWarningVisible(false);
            isFlying = true;
            Rigidbody.bodyType = RigidbodyType2D.Dynamic;
            Rigidbody.gravityScale = 0f;
            Rigidbody.linearVelocity = flightDirection * speed;
        }

        private void FixedUpdate()
        {
            if (!isFlying || isThrown || isConsumed || impactTarget == null) return;

            Vector2 desiredDirection = ((Vector2)impactTarget.position - Rigidbody.position).normalized;
            if (turnSpeed > 0f)
            {
                float blend = 1f - Mathf.Exp(-turnSpeed * Time.fixedDeltaTime);
                flightDirection = Vector2.Lerp(flightDirection, desiredDirection, blend).normalized;
            }

            speed = Mathf.Min(maximumSpeed, speed + acceleration * Time.fixedDeltaTime);
            Rigidbody.linearVelocity = flightDirection * speed;
        }

        private void Update()
        {
            if (isConsumed) return;

            lifeRemaining -= Time.deltaTime;
            if (lifeRemaining <= 0f)
            {
                ReleaseProjectile();
                return;
            }
            if (!isFlying && impactTarget != null && telegraphLine != null)
                telegraphLine.SetEndpoints(transform.position, impactTarget.position);
            if (!isFlying || isThrown || impactTarget == null) return;
            if (Vector2.Distance(transform.position, impactTarget.position) <= impactDistance)
                ImpactMoonSurface();
        }

        protected override void OnGrabbed()
        {
            warningSequence?.Kill();
            isFlying = false;
            isThrown = false;
            SetWarningVisible(false);
        }

        protected override void OnReleased()
        {
            isFlying = false;
            isThrown = false;
        }

        protected override void OnThrown(ThrowData throwData)
        {
            isFlying = true;
            isThrown = true;
            impactTarget = null;
            Rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryImpact(collision.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryImpact(other);
        }

        private void TryImpact(Collider2D targetCollider)
        {
            if (isConsumed || targetCollider == null || IsHeld) return;

            MoonBoss boss = targetCollider.GetComponentInParent<MoonBoss>();
            if (boss != null)
            {
                if (isThrown)
                {
                    boss.TakeDamage(new DamageData(bossDamage, DamageType.Projectile));
                    ReleaseProjectile();
                }
                else
                {
                    ImpactMoonSurface();
                }
                return;
            }

            if (targetCollider.CompareTag("Player") ||
                targetCollider.transform.root.CompareTag("Player"))
            {
                DamageCaster.ApplyDamage(
                    targetCollider.transform,
                    new DamageData(playerDamage, DamageType.Projectile)
                );
                ReleaseProjectile();
            }
        }

        private void ImpactMoonSurface()
        {
            if (isConsumed) return;
            OnBossImpact?.Invoke(transform.position);
            ReleaseProjectile();
        }

        public void ForceRelease()
        {
            ReleaseProjectile();
        }

        private void ReleaseProjectile()
        {
            if (isConsumed) return;
            isConsumed = true;
            warningSequence?.Kill();
            warningSequence = null;
            SetWarningVisible(false);
            Rigidbody.linearVelocity = Vector2.zero;

            if (returnToPool && poolManagerSO != null && PoolItem != null)
                poolManagerSO.Push(this);
            else
                Destroy(gameObject);
        }

        private void SetWarningVisible(bool visible)
        {
            if (telegraphLine != null)
            {
                if (!visible) telegraphLine.Hide();
                return;
            }

            if (lineRenderer != null) lineRenderer.enabled = visible;
        }

        private void OnDisable()
        {
            warningSequence?.Kill();
            warningSequence = null;
            telegraphLine?.Hide();
        }
    }
}
