using System.Collections;
using DG.Tweening;
using Member.KYM.Scripts.Players.RobotArm;
using Member.KYM.Scripts.CombatSystems.DamageSystems;
using KimLIb.ModuleSystems;
using Member.KYM.Scripts.CombatSystems.Projectiles;
using Member.ODK.Scripts.Enemys.Bosses;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Swordmaster
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public class EnchantedSword : MonoBehaviour, IGrabbable, IEnemyAttackGrabbable
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
        [SerializeField, Min(0.05f)] private float summonReturnDuration = 0.6f;
        [SerializeField] private float thrownHitRecallDelay = 0.12f;
        [SerializeField] private float dispelledFlightTime = 1.6f;
        [SerializeField] private float dispelledHarmSpeed = 2f;
        [SerializeField, Min(1f)] private float maximumThrownDistance = 18f;
        [SerializeField, Min(0f)] private float playerGrabPriorityTime = 2.5f;
        [SerializeField, Min(0f)] private float playerHitCooldown = 0.5f;
        [SerializeField, Min(5f)] private float maximumFlightDistance = 60f;

        [Header("Size / Grab")]
        [SerializeField] private float swordLength = 2.6f;
        [SerializeField] private float swordThickness = 0.5f;
        [SerializeField, Min(0f)] private float grabAssistRadius = 1.6f;

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

        public bool CanBeGrabbed => state == SwordState.Dispelled || state == SwordState.MagicFlight;
        public bool IsMagicFlying => state == SwordState.MagicFlight;

        public void Hover(Vector2 facing)
        {
            if (state != SwordState.MagicFlight) return;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            FaceVelocity(facing);
        }
        public Transform GrabTransform => transform;
        public bool IsEnemyAttackFrom(ModuleOwner grabber) => true;
        public bool CanBossControl => state == SwordState.Orbiting || state == SwordState.Recalling;
        public bool CanBossReclaim => state == SwordState.Dispelled &&
                                      Time.time >= bossReclaimTime;
        public bool CanBossForceReclaim => state == SwordState.Dispelled || state == SwordState.MagicFlight;
        public float LaunchTime { get; private set; }
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
        private ModuleOwner throwOwner;
        private Vector2 thrownOrigin;
        private LineRenderer pathLine;
        private float pathTime;
        private float pathDuration;
        private float thrownSpeed;
        private bool thrownHit;
        private float bossReclaimTime;
        private ODKGrabInfoDisplay grabInfoDisplay;
        private CircleCollider2D grabSensor;
        private float nextPlayerHitTime;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.bodyType = RigidbodyType2D.Kinematic;

            swordCollider = GetComponent<CapsuleCollider2D>();
            grabInfoDisplay = GetComponent<ODKGrabInfoDisplay>();
            swordCollider.direction = CapsuleDirection2D.Horizontal;
            swordCollider.size = new Vector2(swordLength, swordThickness);
            swordCollider.isTrigger = true;
            GrabbableLayer.Validate(gameObject);
            if (bladeRenderer == null) bladeRenderer = GetComponentInChildren<SpriteRenderer>();
            if (GetComponent<ProjectileGrabCue>() == null)
                gameObject.AddComponent<ProjectileGrabCue>();
            if (trail == null) trail = GetComponentInChildren<TrailRenderer>();
            if (trail == null) CreateTrail();
            CreateGrabSensor();
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
            if ((state == SwordState.MagicFlight || state == SwordState.Dispelled) && owner != null &&
                body.bodyType == RigidbodyType2D.Dynamic &&
                Vector2.Distance(body.position, owner.transform.position) > maximumFlightDistance)
            {
                body.linearVelocity = Vector2.zero;
            }
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

        public void BlinkHome()
        {
            if (owner == null)
            {
                BeginBossControl();
                return;
            }
            Vector3 from = transform.position;
            BeginBossControl();
            HidePath();
            transform.SetParent(originalParent, true);
            Vector3 destination = owner.GetOrbitPosition(orbitIndex);
            transform.position = destination;
            transform.rotation = Quaternion.Euler(0f, 0f, owner.GetOrbitAngle(orbitIndex) + 90f);
            owner.NotifySwordBlink(from, destination);
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
            PrepareForPlayerGrab();
            owner?.NotifySwordDispelled(transform.position);
        }

        public bool IsHome => state == SwordState.Orbiting;
        public bool IsReturning => state == SwordState.Recalling;
        public bool CanBeSummoned => state == SwordState.Dispelled ||
                                     state == SwordState.BossControlled ||
                                     state == SwordState.MagicFlight;

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

        public void FireMagic(
            Vector2 direction,
            float speed,
            float lifeTime = -1f,
            bool showPath = true)
        {
            StopStateRoutine();
            transform.DOKill();
            state = SwordState.MagicFlight;
            LaunchTime = Time.time;
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
            LaunchTime = Time.time;
            ClearTrail();
            transform.SetParent(null, true);
            SetEnchanted(false);
            SetPhysics(true, direction.normalized * speed);
            PrepareForPlayerGrab();
            FaceVelocity(direction);
            ShowPath(direction.normalized * speed, 0f, dispelledFlightTime);
            owner?.NotifySwordDispelled(transform.position);

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
            grabInfoDisplay?.SetHeld(true);
            throwOwnerRoot = grabber != null ? grabber.transform.root : null;
            throwOwner = null;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            swordCollider.enabled = false;
            if (grabSensor != null) grabSensor.enabled = false;
            transform.SetParent(grabPoint, true);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        public void Release()
        {
            if (state != SwordState.Held) return;
            transform.SetParent(null, true);
            state = SwordState.Dispelled;
            grabInfoDisplay?.SetHeld(false);
            throwOwnerRoot = null;
            throwOwner = null;
            swordCollider.enabled = true;
            SetPhysics(true, Vector2.zero);
            PrepareForPlayerGrab();
            StopStateRoutine();
        }

        public void Throw(ThrowData throwData)
        {
            if (state != SwordState.Held) return;
            transform.SetParent(null, true);
            state = SwordState.PlayerThrown;
            grabInfoDisplay?.SetHeld(false);
            thrownHit = false;
            thrownSpeed = Mathf.Max(8f, throwData.ArmThrowSpeed);
            thrownOrigin = body.position;
            throwOwnerRoot = throwData.Owner != null ? throwData.Owner.transform.root : throwOwnerRoot;
            throwOwner = throwData.Owner;
            swordCollider.enabled = true;
            SetPhysics(true, throwData.Direction * thrownSpeed);
            body.gravityScale = 0f;
            FaceVelocity(throwData.Direction);
        }

        private void OnTriggerEnter2D(Collider2D other) => HandleImpact(other);
        private void OnTriggerStay2D(Collider2D other) => HandleImpact(other);
        private void OnCollisionEnter2D(Collision2D collision) => HandleImpact(collision.collider);

        private void HandleImpact(Collider2D other)
        {
            if (other == null || owner == null) return;
            if (other.transform.IsChildOf(transform)) return;
            if (grabSensor != null && swordCollider != null && swordCollider.enabled)
            {
                ColliderDistance2D distance = swordCollider.Distance(other);
                if (!distance.isValid || distance.distance > 0.05f) return;
            }
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
                    PlayerDamageFeedback.Apply(throwOwner, owner,
                        new DamageData(bossDamage, DamageType.Projectile), transform.position);
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
                if (hitPlayer) HitPlayer(other);
                return;
            }

            if (state == SwordState.Dispelled)
            {
                bool moving = body.linearVelocity.magnitude >= dispelledHarmSpeed;
                if (!moving) return;
                if (hitPlayer) HitPlayer(other);
                return;
            }

        }

        private IEnumerator MagicFlightRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            stateRoutine = null;
            if (state == SwordState.MagicFlight) DisenchantKeepFlying();
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
            Vector2 origin = body != null ? body.position : (Vector2)transform.position;
            int count = Mathf.Max(2, pathSegments);
            Vector3[] points = new Vector3[count + 1];
            points[0] = new Vector3(origin.x, origin.y, transform.position.z);
            int used = 1;
            for (int i = 1; i <= count; i++)
            {
                float t = duration * i / count;
                Vector2 point = origin + velocity * t + 0.5f * gravity * t * t;
                points[used++] = new Vector3(point.x, point.y, transform.position.z);
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
            ClearTrail();
            SetEnchanted(true);
            SetPhysics(false, Vector2.zero);
            swordCollider.enabled = false;
            transform.SetParent(originalParent, true);
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            float elapsed = 0f;
            while (state == SwordState.Recalling && owner != null && elapsed < summonReturnDuration)
            {
                elapsed += Time.deltaTime;
                float rate = Mathf.Clamp01(elapsed / summonReturnDuration);
                float eased = rate * rate * (3f - 2f * rate);
                Vector3 destination = owner.GetOrbitPosition(orbitIndex);
                Vector3 toHome = destination - transform.position;
                transform.position = Vector3.LerpUnclamped(startPosition, destination, eased);
                if (toHome.sqrMagnitude > 0.0001f)
                {
                    float angle = Mathf.Atan2(toHome.y, toHome.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Slerp(startRotation, Quaternion.Euler(0f, 0f, angle), Mathf.Clamp01(rate * 4f));
                }
                yield return null;
            }
            stateRoutine = null;
            if (state == SwordState.Recalling)
            {
                transform.position = owner.GetOrbitPosition(orbitIndex);
                EnterOrbit();
            }
        }

        private void SetPhysics(bool dynamicBody, Vector2 velocity)
        {
            swordCollider.enabled = true;
            if (grabSensor != null) grabSensor.enabled = true;
            body.bodyType = dynamicBody ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.linearVelocity = velocity;
            body.angularVelocity = 0f;
        }

        private void HitPlayer(Collider2D other)
        {
            if (Time.time < nextPlayerHitTime) return;
            if (!DamageCaster.IsWithinPlayerHitbox(swordCollider, other)) return;
            nextPlayerHitTime = Time.time + playerHitCooldown;
            DamageCaster.ApplyDamage(other.transform, new DamageData(playerDamage, DamageType.Projectile));
            owner.NotifySwordImpact(transform.position);
        }

        private void DisenchantKeepFlying()
        {
            Vector2 velocity = body.linearVelocity;
            state = SwordState.Dispelled;
            SetEnchanted(false);
            PrepareForPlayerGrab();
            body.linearVelocity = velocity;
            owner?.NotifySwordDispelled(transform.position);
        }

        private void CreateGrabSensor()
        {
            if (grabSensor != null || grabAssistRadius <= 0f) return;
            GameObject sensorObject = new GameObject("Grab Sensor");
            sensorObject.transform.SetParent(transform, false);
            int grabbableLayer = GrabbableLayer.Index;
            if (grabbableLayer >= 0) sensorObject.layer = grabbableLayer;
            grabSensor = sensorObject.AddComponent<CircleCollider2D>();
            grabSensor.isTrigger = true;
            grabSensor.radius = grabAssistRadius;
        }

        private void PrepareForPlayerGrab()
        {
            int grabbableLayer = GrabbableLayer.Index;
            if (grabbableLayer >= 0)
            {
                gameObject.layer = grabbableLayer;
                foreach (Transform child in transform)
                    child.gameObject.layer = grabbableLayer;
            }

            swordCollider.enabled = true;
            body.simulated = true;
            bossReclaimTime = Time.time + playerGrabPriorityTime;
            Physics2D.SyncTransforms();
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
            grabInfoDisplay?.SetHeld(false);
            transform.DOKill();
            StopStateRoutine();
            HidePath();
            ClearTrail();
        }
    }
}
