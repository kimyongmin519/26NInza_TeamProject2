using System.Collections;
using DG.Tweening;
using Member.KYM.Scripts.Players.RobotArm;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Swordmaster
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public class EnchantedSword : MonoBehaviour, IGrabbable
    {
        private enum SwordState
        {
            Orbiting,
            BossControlled,
            MagicFlight,
            Dispelled,
            Held,
            PlayerThrown,
            Recalling
        }

        [Header("Flight")]
        [SerializeField] private float recallDuration = 0.32f;
        [SerializeField] private float thrownRecallDelay = 1.7f;
        [SerializeField] private float magicFlightLife = 1.8f;
        [SerializeField] private float dispelledRecallDelay = 6f;

        [Header("Placeholder")]
        [SerializeField] private Color enchantedColor = new Color(0.75f, 0.25f, 1f, 1f);
        [SerializeField] private Color dispelledColor = new Color(0.75f, 0.78f, 0.85f, 1f);
        [SerializeField] private LineRenderer blade;
        [SerializeField] private LineRenderer guard;

        public bool CanBeGrabbed => state == SwordState.Dispelled;
        public Transform GrabTransform => transform;
        public bool CanBossControl => state == SwordState.Orbiting || state == SwordState.Recalling;
        public bool IsMagicLocked => state != SwordState.Dispelled &&
                                     state != SwordState.Held &&
                                     state != SwordState.PlayerThrown;

        private Swordmaster owner;
        private Rigidbody2D body;
        private CapsuleCollider2D swordCollider;
        private SwordState state;
        private int orbitIndex;
        private float playerDamage;
        private float bossDamage;
        private LayerMask playerLayer;
        private LayerMask groundLayer;
        private Coroutine stateRoutine;
        private Transform originalParent;
        private Transform throwOwnerRoot;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.bodyType = RigidbodyType2D.Kinematic;

            swordCollider = GetComponent<CapsuleCollider2D>();
            swordCollider.direction = CapsuleDirection2D.Horizontal;
            swordCollider.size = new Vector2(1.75f, 0.36f);
            swordCollider.isTrigger = true;
            GrabbableLayer.TryApply(gameObject);
            if (blade == null) blade = GetComponent<LineRenderer>();
            if (guard == null)
            {
                Transform guardTransform = transform.Find("Guard");
                if (guardTransform != null) guard = guardTransform.GetComponent<LineRenderer>();
            }
            SetEnchanted(true);
        }

        public void Initialize(
            Swordmaster swordOwner,
            int index,
            float damageToPlayer,
            float damageToBoss,
            LayerMask playerMask,
            LayerMask groundMask)
        {
            owner = swordOwner;
            orbitIndex = index;
            playerDamage = damageToPlayer;
            bossDamage = damageToBoss;
            playerLayer = playerMask;
            groundLayer = groundMask;
            originalParent = swordOwner.transform;
            transform.SetParent(originalParent, true);
            EnterOrbit();
        }

        private void LateUpdate()
        {
            if (owner == null || state != SwordState.Orbiting) return;
            transform.position = owner.GetOrbitPosition(orbitIndex);
            float angle = owner.GetOrbitAngle(orbitIndex);
            transform.rotation = Quaternion.Euler(0f, 0f, angle + 90f);
            int order = Mathf.RoundToInt(20f - transform.position.z * 5f);
            if (blade != null) blade.sortingOrder = order;
            if (guard != null) guard.sortingOrder = order + 1;
        }

        public void BeginBossControl()
        {
            StopStateRoutine();
            transform.DOKill();
            state = SwordState.BossControlled;
            SetEnchanted(true);
            SetPhysics(false, Vector2.zero);
        }

        public Tween MoveTo(Vector3 position, float angle, float duration, Ease ease = Ease.OutCubic)
        {
            BeginBossControl();
            Sequence sequence = DOTween.Sequence().SetTarget(this);
            sequence.Join(transform.DOMove(position, duration).SetEase(ease));
            sequence.Join(transform.DORotate(new Vector3(0f, 0f, angle), duration).SetEase(ease));
            return sequence;
        }

        public void FireMagic(Vector2 direction, float speed, float lifeTime = -1f)
        {
            StopStateRoutine();
            transform.DOKill();
            state = SwordState.MagicFlight;
            SetEnchanted(true);
            SetPhysics(true, direction.normalized * speed);
            FaceVelocity(direction);
            owner?.NotifySwordLaunch(transform.position);
            stateRoutine = StartCoroutine(MagicFlightRoutine(lifeTime > 0f ? lifeTime : magicFlightLife));
        }

        public void FireDispelled(Vector2 direction, float speed)
        {
            StopStateRoutine();
            transform.DOKill();
            state = SwordState.Dispelled;
            SetEnchanted(false);
            SetPhysics(true, direction.normalized * speed);
            body.gravityScale = 0.55f;
            FaceVelocity(direction);
            owner?.NotifySwordDispelled(transform.position);
            stateRoutine = StartCoroutine(DispelledRecallRoutine());
        }

        public void Recall()
        {
            if (owner == null || state == SwordState.Held) return;
            StopStateRoutine();
            transform.DOKill();
            state = SwordState.Recalling;
            SetEnchanted(true);
            SetPhysics(false, Vector2.zero);
            transform.SetParent(originalParent, true);
            Vector3 destination = owner.GetOrbitPosition(orbitIndex);
            transform.DOMove(destination, recallDuration)
                .SetEase(Ease.InOutCubic)
                .SetTarget(this)
                .OnComplete(EnterOrbit);
        }

        private void EnterOrbit()
        {
            transform.DOKill();
            state = SwordState.Orbiting;
            SetEnchanted(true);
            SetPhysics(false, Vector2.zero);
            transform.SetParent(originalParent, true);
            owner?.NotifySwordRecalled(transform.position);
        }

        public void Grab(Transform grabPoint, GameObject grabber)
        {
            if (!CanBeGrabbed || grabPoint == null) return;
            StopStateRoutine();
            transform.DOKill();
            state = SwordState.Held;
            throwOwnerRoot = grabber != null ? grabber.transform.root : null;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            swordCollider.enabled = false;
            transform.SetParent(grabPoint, true);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        public void Release()
        {
            if (state != SwordState.Held) return;
            transform.SetParent(originalParent, true);
            state = SwordState.Dispelled;
            throwOwnerRoot = null;
            swordCollider.enabled = true;
            SetPhysics(true, Vector2.zero);
            body.gravityScale = 0.75f;
        }

        public void Throw(ThrowData throwData)
        {
            if (state != SwordState.Held) return;
            transform.SetParent(originalParent, true);
            state = SwordState.PlayerThrown;
            throwOwnerRoot = throwData.Owner != null ? throwData.Owner.transform.root : throwOwnerRoot;
            swordCollider.enabled = true;
            SetPhysics(true, throwData.Direction * throwData.ArmThrowSpeed);
            body.gravityScale = 0.15f;
            FaceVelocity(throwData.Direction);
            stateRoutine = StartCoroutine(ThrownRecallRoutine());
        }

        private void OnTriggerEnter2D(Collider2D other) => HandleImpact(other);
        private void OnCollisionEnter2D(Collision2D collision) => HandleImpact(collision.collider);

        private void HandleImpact(Collider2D other)
        {
            if (other == null || owner == null) return;
            if (other.GetComponentInParent<EnchantedSword>() != null) return;
            if (state == SwordState.PlayerThrown && throwOwnerRoot != null &&
                other.transform.root == throwOwnerRoot)
                return;
            Swordmaster hitBoss = other.GetComponentInParent<Swordmaster>();
            if (hitBoss == owner)
            {
                if (state == SwordState.PlayerThrown)
                {
                    owner.TakeDamage(new DamageData(bossDamage, DamageType.Projectile));
                    Recall();
                }
                return;
            }
            int mask = 1 << other.gameObject.layer;
            bool hitPlayer = (playerLayer.value & mask) != 0 || other.transform.root.CompareTag("Player");
            bool hitGround = (groundLayer.value & mask) != 0;

            if (state == SwordState.MagicFlight)
            {
                if (hitPlayer)
                    DamageCaster.ApplyDamage(other.transform, new DamageData(playerDamage, DamageType.Projectile));
                if (hitPlayer || hitGround) Recall();
                return;
            }

            if (state == SwordState.Dispelled)
            {
                if (hitPlayer)
                    DamageCaster.ApplyDamage(other.transform, new DamageData(playerDamage, DamageType.Projectile));
                if (hitPlayer || hitGround)
                {
                    body.linearVelocity = Vector2.zero;
                    body.angularVelocity = 0f;
                }
                return;
            }

            if (state != SwordState.PlayerThrown) return;
            Recall();
        }

        private IEnumerator MagicFlightRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            Recall();
        }

        private IEnumerator ThrownRecallRoutine()
        {
            yield return new WaitForSeconds(thrownRecallDelay);
            Recall();
        }

        private IEnumerator DispelledRecallRoutine()
        {
            yield return new WaitForSeconds(dispelledRecallDelay);
            if (state == SwordState.Dispelled) Recall();
        }

        private void SetPhysics(bool dynamicBody, Vector2 velocity)
        {
            swordCollider.enabled = true;
            body.bodyType = dynamicBody ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.linearVelocity = velocity;
            body.angularVelocity = 0f;
        }

        private void FaceVelocity(Vector2 direction)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon) return;
            transform.rotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg
            );
        }

        private void SetEnchanted(bool enchanted)
        {
            Color color = enchanted ? enchantedColor : dispelledColor;
            if (blade != null)
            {
                blade.startColor = color;
                blade.endColor = enchanted
                    ? new Color(1f, 0.75f, 1f, 1f)
                    : new Color(0.95f, 0.95f, 1f, 1f);
            }
            if (guard != null)
            {
                guard.startColor = color;
                guard.endColor = color;
            }
        }

        private void StopStateRoutine()
        {
            if (stateRoutine == null) return;
            StopCoroutine(stateRoutine);
            stateRoutine = null;
        }

        private void OnDisable()
        {
            transform.DOKill();
            StopStateRoutine();
        }
    }
}
