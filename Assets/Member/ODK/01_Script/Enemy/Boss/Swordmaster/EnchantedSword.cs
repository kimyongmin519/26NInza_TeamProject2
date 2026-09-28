using System.Collections;
using DG.Tweening;
using Member.KYM.Scripts.Players.RobotArm;
using Member.ODK.Scripts.Enemys.Bosses;
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
        [SerializeField] private float thrownHitWindow = 2.6f;
        [SerializeField] private float thrownHomingDegrees = 1080f;
        [SerializeField] private float returnStartSpeed = 4f;
        [SerializeField] private float returnMaxSpeed = 34f;
        [SerializeField] private float returnAcceleration = 70f;
        [SerializeField] private float thrownHitRecallDelay = 0.12f;
        [SerializeField] private float dispelledFlightTime = 1.6f;
        [SerializeField] private float dispelledHarmSpeed = 2f;
        [SerializeField, Min(1f)] private float maximumThrownDistance = 18f;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer bladeRenderer;
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private Color enchantedColor = new Color(0.9f, 0.7f, 1f, 1f);
        [SerializeField] private Color dispelledColor = new Color(0.72f, 0.74f, 0.8f, 1f);
        [SerializeField] private Color trailColor = new Color(0.75f, 0.3f, 1f, 0.75f);
        [SerializeField] private float trailTime = 0.14f;
        [SerializeField] private float trailWidth = 0.22f;

        [Header("Path Line")]
        [SerializeField] private Color pathColor = new Color(1f, 1f, 1f, 0.32f);
        [SerializeField] private float pathWidth = 0.07f;
        [SerializeField, Min(2)] private int pathSegments = 24;

        public bool CanBeGrabbed => state == SwordState.Dispelled;
        public Transform GrabTransform => transform;
        public bool CanBossControl => state == SwordState.Orbiting || state == SwordState.Recalling;
        public bool CanBossReclaim => state == SwordState.Dispelled;
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
        private Vector2 thrownOrigin;
        private LineRenderer pathLine;
        private float pathTime;
        private float pathDuration;
        private float thrownSpeed;
        private bool thrownHit;

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
            if (bladeRenderer == null) bladeRenderer = GetComponentInChildren<SpriteRenderer>();
            if (trail == null) trail = GetComponentInChildren<TrailRenderer>();
            if (trail == null) CreateTrail();
            CreatePathLine();
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
            bossDamage = Mathf.Clamp(damageToBoss, 0f, 1f);
            playerLayer = playerMask;
            groundLayer = groundMask;
            originalParent = swordOwner.transform;
            transform.SetParent(originalParent, true);
            EnterOrbit();
        }

        private void FixedUpdate()
        {
            if (state != SwordState.PlayerThrown || thrownHit || owner == null) return;
            if (Vector2.Distance(thrownOrigin, body.position) >= maximumThrownDistance)
            {
                Recall();
                return;
            }
            Vector2 toBoss = owner.GetHitCenter() - body.position;
            if (toBoss.sqrMagnitude < 0.0001f) return;
            Vector2 current = body.linearVelocity.sqrMagnitude > 0.01f ? body.linearVelocity.normalized : toBoss.normalized;
            Vector2 next = Vector3.RotateTowards(current, toBoss.normalized, thrownHomingDegrees * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f);
            body.linearVelocity = next.normalized * Mathf.Max(thrownSpeed, body.linearVelocity.magnitude);
            FaceVelocity(next);
        }

        private void Update()
        {
            if (pathLine == null || !pathLine.enabled) return;
            pathTime += Time.deltaTime;
            float rate = pathDuration > 0f ? Mathf.Clamp01(pathTime / pathDuration) : 1f;
            Color start = pathColor;
            start.a *= 1f - rate;
            Color end = start;
            end.a *= 0.35f;
            pathLine.startColor = start;
            pathLine.endColor = end;
            if (rate >= 1f) HidePath();
        }

        private void LateUpdate()
        {
            if (owner == null) return;
            int bodyOrder = owner.BodySortingOrder;
            bool behind = false;
            if (state == SwordState.Orbiting)
            {
                transform.position = owner.GetOrbitPosition(orbitIndex);
                float angle = owner.GetOrbitAngle(orbitIndex);
                transform.rotation = Quaternion.Euler(0f, 0f, angle + 90f);
                behind = transform.position.z - owner.transform.position.z > 0.001f;
            }
            else if (state == SwordState.Recalling)
            {
                behind = owner.GetOrbitPosition(orbitIndex).z - owner.transform.position.z > 0.001f;
            }
            int order = behind ? bodyOrder - 2 : bodyOrder + 2;
            if (bladeRenderer != null) bladeRenderer.sortingOrder = order;
            if (trail != null) trail.sortingOrder = order - 1;
        }

        public void BeginBossControl()
        {
            StopStateRoutine();
            transform.DOKill();
            state = SwordState.BossControlled;
            ClearTrail();
            SetEnchanted(true);
            SetPhysics(false, Vector2.zero);
        }

        public void EndBossControl()
        {
            if (state == SwordState.BossControlled) Expend();
        }

        public void Expend()
        {
            StopStateRoutine();
            transform.DOKill();
            state = SwordState.Dispelled;
            transform.SetParent(null, true);
            SetEnchanted(false);
            SetPhysics(true, Vector2.zero);
            owner?.NotifySwordDispelled(transform.position);
        }

        public bool IsHome => state == SwordState.Orbiting;
        public bool IsReturning => state == SwordState.Recalling;
        public bool CanBeSummoned => state == SwordState.Dispelled || state == SwordState.BossControlled;

        public void Summon()
        {
            if (!CanBeSummoned || owner == null) return;
            StopStateRoutine();
            transform.DOKill();
            stateRoutine = StartCoroutine(ReturnRoutine());
        }

        public void ShowPathLine(Vector3 from, Vector3 to, float duration)
        {
            if (pathLine == null) CreatePathLine();
            if (pathLine == null || duration <= 0f) return;
            pathLine.positionCount = 2;
            pathLine.SetPosition(0, from);
            pathLine.SetPosition(1, to);
            pathLine.widthMultiplier = pathWidth;
            pathLine.startColor = pathColor;
            pathLine.endColor = pathColor;
            pathTime = 0f;
            pathDuration = duration;
            pathLine.enabled = true;
        }

        public Tween MoveTo(Vector3 position, float angle, float duration, Ease ease = Ease.OutCubic)
        {
            BeginBossControl();
            Sequence sequence = DOTween.Sequence().SetTarget(this);
            sequence.Join(transform.DOMove(position, duration).SetEase(ease));
            sequence.Join(transform.DORotate(new Vector3(0f, 0f, angle), duration).SetEase(ease));
            return sequence;
        }

        public void FireMagic(Vector2 direction, float speed, float lifeTime = -1f, bool showPath = true)
        {
            StopStateRoutine();
            transform.DOKill();
            state = SwordState.MagicFlight;
            ClearTrail();
            transform.SetParent(null, true);
            SetEnchanted(true);
            SetPhysics(true, direction.normalized * speed);
            FaceVelocity(direction);
            float life = lifeTime > 0f ? lifeTime : magicFlightLife;
            if (showPath) ShowPath(direction.normalized * speed, 0f, life);
            else HidePath();
            owner?.NotifySwordLaunch(transform.position);
            stateRoutine = StartCoroutine(MagicFlightRoutine(life));
        }

        public void FireDispelled(Vector2 direction, float speed)
        {
            StopStateRoutine();
            transform.DOKill();
            state = SwordState.Dispelled;
            ClearTrail();
            transform.SetParent(null, true);
            SetEnchanted(false);
            SetPhysics(true, direction.normalized * speed);
            FaceVelocity(direction);
            ShowPath(direction.normalized * speed, 0f, dispelledFlightTime);
            owner?.NotifySwordDispelled(transform.position);
            stateRoutine = StartCoroutine(StopDispelledFlight(dispelledFlightTime));
        }

        public void Recall() => Recall(recallDuration);

        public void Recall(float duration)
        {
            if (owner == null || state == SwordState.Held) return;
            StopStateRoutine();
            transform.DOKill();
            HidePath();
            state = SwordState.Recalling;
            ClearTrail();
            SetEnchanted(true);
            SetPhysics(false, Vector2.zero);
            transform.SetParent(originalParent, true);
            Vector3 destination = owner.GetOrbitPosition(orbitIndex);
            transform.DOMove(destination, Mathf.Max(0.02f, duration))
                .SetEase(Ease.InOutCubic)
                .SetTarget(this)
                .OnComplete(EnterOrbit);
        }

        private void EnterOrbit()
        {
            transform.DOKill();
            state = SwordState.Orbiting;
            ClearTrail();
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
            HidePath();
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
            transform.SetParent(null, true);
            state = SwordState.Dispelled;
            throwOwnerRoot = null;
            swordCollider.enabled = true;
            SetPhysics(true, Vector2.zero);
            StopStateRoutine();
        }

        public void Throw(ThrowData throwData)
        {
            if (state != SwordState.Held) return;
            transform.SetParent(null, true);
            state = SwordState.PlayerThrown;
            thrownHit = false;
            thrownSpeed = Mathf.Max(8f, throwData.ArmThrowSpeed);
            thrownOrigin = body.position;
            throwOwnerRoot = throwData.Owner != null ? throwData.Owner.transform.root : throwOwnerRoot;
            swordCollider.enabled = true;
            SetPhysics(true, throwData.Direction * thrownSpeed);
            body.gravityScale = 0f;
            FaceVelocity(throwData.Direction);
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
                if (state == SwordState.PlayerThrown && !thrownHit)
                {
                    thrownHit = true;
                    owner.TakeDamage(new DamageData(bossDamage, DamageType.Projectile));
                    owner.NotifySwordImpact(transform.position);
                    StopStateRoutine();
                    stateRoutine = StartCoroutine(RecallAfter(thrownHitRecallDelay));
                }
                return;
            }
            int mask = 1 << other.gameObject.layer;
            bool hitPlayer = (playerLayer.value & mask) != 0 || other.transform.root.CompareTag("Player");

            if (state == SwordState.MagicFlight)
            {
                if (hitPlayer)
                    DamageCaster.ApplyDamage(other.transform, new DamageData(playerDamage, DamageType.Projectile));
                if (hitPlayer)
                {
                    owner.NotifySwordImpact(transform.position);
                    Expend();
                }
                return;
            }

            if (state == SwordState.Dispelled)
            {
                bool moving = body.linearVelocity.magnitude >= dispelledHarmSpeed;
                if (!moving) return;
                if (hitPlayer)
                    DamageCaster.ApplyDamage(other.transform, new DamageData(playerDamage, DamageType.Projectile));
                if (hitPlayer)
                {
                    owner.NotifySwordImpact(transform.position);
                    body.linearVelocity = Vector2.zero;
                    body.angularVelocity = 0f;
                }
                return;
            }

        }

        private IEnumerator MagicFlightRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            stateRoutine = null;
            if (state == SwordState.MagicFlight) Expend();
        }

        private IEnumerator ThrownRecallRoutine()
        {
            yield return new WaitForSeconds(Mathf.Max(thrownRecallDelay, thrownHitWindow));
            Recall();
        }

        private IEnumerator RecallAfter(float delay)
        {
            body.linearVelocity *= 0.25f;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            stateRoutine = null;
            Recall();
        }

        private void CreatePathLine()
        {
            if (pathLine != null) return;
            GameObject lineObject = new GameObject("Path Line");
            lineObject.transform.SetParent(transform, false);
            pathLine = lineObject.AddComponent<LineRenderer>();
            pathLine.useWorldSpace = true;
            pathLine.widthMultiplier = pathWidth;
            pathLine.numCapVertices = 2;
            pathLine.sortingOrder = 5;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) pathLine.material = new Material(shader);
            pathLine.enabled = false;
        }

        private void ShowPath(Vector2 velocity, float gravityScale, float duration)
        {
            if (pathLine == null) CreatePathLine();
            if (pathLine == null || duration <= 0f) return;
            Vector2 gravity = Physics2D.gravity * gravityScale;
            Vector2 previous = body != null ? body.position : (Vector2)transform.position;
            Vector2 origin = previous;
            int count = Mathf.Max(2, pathSegments);
            Vector3[] points = new Vector3[count + 1];
            points[0] = new Vector3(origin.x, origin.y, transform.position.z);
            int used = 1;
            for (int i = 1; i <= count; i++)
            {
                float t = duration * i / count;
                Vector2 point = origin + velocity * t + 0.5f * gravity * t * t;
                points[used++] = new Vector3(point.x, point.y, transform.position.z);
                previous = point;
            }
            pathLine.positionCount = used;
            for (int i = 0; i < used; i++) pathLine.SetPosition(i, points[i]);
            pathLine.widthMultiplier = pathWidth;
            pathLine.startColor = pathColor;
            pathLine.endColor = pathColor;
            pathTime = 0f;
            pathDuration = duration;
            pathLine.enabled = true;
        }

        private void HidePath()
        {
            if (pathLine != null) pathLine.enabled = false;
        }

        private IEnumerator StopDispelledFlight(float duration)
        {
            yield return new WaitForSeconds(duration);
            stateRoutine = null;
            if (state != SwordState.Dispelled) yield break;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        private IEnumerator ReturnRoutine()
        {
            if (owner == null)
            {
                stateRoutine = null;
                yield break;
            }

            HidePath();
            state = SwordState.Recalling;
            SetEnchanted(true);
            float speed = Mathf.Max(returnStartSpeed, body.linearVelocity.magnitude);
            SetPhysics(false, Vector2.zero);
            swordCollider.enabled = false;
            while (state == SwordState.Recalling && owner != null)
            {
                Vector3 destination = owner.GetOrbitPosition(orbitIndex);
                Vector3 toHome = destination - transform.position;
                speed = Mathf.Min(returnMaxSpeed, speed + returnAcceleration * Time.deltaTime);
                float step = speed * Time.deltaTime;
                if (toHome.magnitude <= step + 0.05f)
                {
                    transform.position = destination;
                    break;
                }
                transform.position += toHome.normalized * step;
                float angle = Mathf.Atan2(toHome.y, toHome.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }
            stateRoutine = null;
            if (state == SwordState.Recalling) EnterOrbit();
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

        private void CreateTrail()
        {
            Transform parent = bladeRenderer != null ? bladeRenderer.transform.parent : transform;
            GameObject trailObject = new GameObject("Trail");
            trailObject.transform.SetParent(parent != null ? parent : transform, false);
            trail = trailObject.AddComponent<TrailRenderer>();
            trail.time = trailTime;
            trail.minVertexDistance = 0.05f;
            trail.widthMultiplier = trailWidth;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.numCapVertices = 2;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) trail.material = new Material(shader);
            Color end = trailColor;
            end.a = 0f;
            trail.startColor = trailColor;
            trail.endColor = end;
        }

        private void SetEnchanted(bool enchanted)
        {
            if (bladeRenderer != null) bladeRenderer.color = enchanted ? enchantedColor : dispelledColor;
            if (trail != null) trail.emitting = enchanted;
        }

        private void ClearTrail()
        {
            if (trail == null) return;
            trail.emitting = false;
            trail.Clear();
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
            HidePath();
            ClearTrail();
        }
    }
}
