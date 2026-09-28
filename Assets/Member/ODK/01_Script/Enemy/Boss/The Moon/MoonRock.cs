using GGMLib.ObjectPool.Runtime;
using KimLIb.ObjectPool.Runtime;
using Member.KYM.Scripts.Players.RobotArm;
using Member.ODK.Scripts.Enemys.Bosses;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class MoonRock : GrabbableRigidbody, IPoolable, ICancellableBossSpawn
    {
        [SerializeField] private float armDelay = 0.18f;
        [SerializeField, Min(1f)] private float maximumThrownDistance = 18f;
        [SerializeField] private DamageCaster explosionCaster;

        [Header("Grab Assist")]
        [SerializeField, Min(0f)] private float grabAssistRadius = 1.1f;

        public PoolItemSO PoolItem { get; set; }
        public GameObject GameObject => this != null ? gameObject : null;

        private MoonBoss owner;
        private float bossDamage;
        private float playerDamage;
        private float explosionRadius;
        private float lifeRemaining;
        private float armedTime;
        private LayerMask playerLayer;
        private bool isThrown;
        private bool exploded;
        private Vector2 previousPosition;
        private Vector2 thrownOrigin;
        private Collider2D bodyCollider;
        private CircleCollider2D grabSensor;
        private CollisionDetectionMode2D defaultDetectionMode;
        private float defaultGravityScale;
        private Vector3 defaultScale;
        private ODKGrabInfoDisplay grabInfoDisplay;

        protected override void Awake()
        {
            bodyCollider = GetComponent<Collider2D>();
            grabInfoDisplay = GetComponent<ODKGrabInfoDisplay>();
            CreateGrabSensor();
            base.Awake();
            defaultDetectionMode = Rigidbody.collisionDetectionMode;
            defaultGravityScale = Rigidbody.gravityScale;
            defaultScale = transform.localScale;
        }

        private void CreateGrabSensor()
        {
            if (grabAssistRadius <= 0f || grabSensor != null) return;
            GameObject sensorObject = new GameObject("Grab Sensor");
            sensorObject.transform.SetParent(transform, false);
            int grabbableLayer = GrabbableLayer.Index;
            if (grabbableLayer >= 0)
                sensorObject.layer = grabbableLayer;
            GrabbableLayer.Validate(sensorObject);
            grabSensor = sensorObject.AddComponent<CircleCollider2D>();
            grabSensor.isTrigger = true;
            UpdateGrabSensorRadius();
        }

        private void UpdateGrabSensorRadius()
        {
            if (grabSensor == null) return;
            Vector3 lossy = transform.lossyScale;
            float scale = Mathf.Max(0.0001f, Mathf.Max(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y)));
            grabSensor.radius = grabAssistRadius / scale;
        }

        public void ResetItem()
        {
            owner = null;
            isThrown = false;
            exploded = false;
            Rigidbody.simulated = true;
            Rigidbody.bodyType = RigidbodyType2D.Dynamic;
            Rigidbody.collisionDetectionMode = defaultDetectionMode;
            Rigidbody.linearVelocity = Vector2.zero;
            Rigidbody.angularVelocity = 0f;
            Rigidbody.gravityScale = defaultGravityScale;
            transform.localScale = defaultScale;
            transform.rotation = Quaternion.identity;
            explosionCaster?.DisableCasting();
            if (bodyCollider != null) bodyCollider.enabled = true;
            if (grabSensor != null) grabSensor.enabled = true;
            grabInfoDisplay?.SetHeld(false);
        }

        public void Initialize(
            MoonBoss moonBoss,
            Vector2 velocity,
            float damageToBoss,
            float damageToPlayer,
            float blastRadius,
            float lifeTime,
            LayerMask playerMask,
            float gravityScale = -1f)
        {
            owner = moonBoss;
            bossDamage = Mathf.Clamp(damageToBoss, 0f, 1f);
            playerDamage = Mathf.Max(0f, damageToPlayer);
            explosionRadius = Mathf.Max(0.1f, blastRadius);
            lifeRemaining = Mathf.Max(0.5f, lifeTime);
            playerLayer = playerMask;
            armedTime = Time.time + Mathf.Max(0f, armDelay);
            isThrown = false;
            exploded = false;
            Rigidbody.simulated = true;
            if (gravityScale > 0f) Rigidbody.gravityScale = gravityScale;
            Rigidbody.linearVelocity = velocity;
            Rigidbody.angularVelocity = Random.Range(-300f, 300f);
            previousPosition = Rigidbody.position;
            UpdateGrabSensorRadius();
        }

        private void Update()
        {
            if (exploded || IsHeld) return;
            lifeRemaining -= Time.deltaTime;
            if (lifeRemaining <= 0f) Despawn();
        }

        private void FixedUpdate()
        {
            if (!isThrown || exploded || IsHeld)
            {
                previousPosition = Rigidbody.position;
                return;
            }

            if (Vector2.Distance(thrownOrigin, Rigidbody.position) >= maximumThrownDistance)
            {
                Despawn();
                return;
            }

            Vector2 currentPosition = Rigidbody.position;
            foreach (RaycastHit2D hit in Physics2D.LinecastAll(previousPosition, currentPosition))
            {
                if (hit.collider == null || hit.rigidbody == Rigidbody) continue;
                HandleContact(hit.collider, hit.point, hit.normal);
                if (exploded) break;
            }
            previousPosition = currentPosition;
        }

        protected override void OnGrabbed()
        {
            isThrown = false;
            previousPosition = Rigidbody.position;
            grabInfoDisplay?.SetHeld(true);
        }

        protected override void OnReleased()
        {
            isThrown = false;
            previousPosition = Rigidbody.position;
            grabInfoDisplay?.SetHeld(false);
        }

        protected override void OnThrown(ThrowData throwData)
        {
            isThrown = true;
            thrownOrigin = Rigidbody.position;
            armedTime = Time.time;
            Rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            previousPosition = Rigidbody.position;
            grabInfoDisplay?.SetHeld(false);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            Vector2 point = transform.position;
            Vector2 normal = Vector2.up;
            if (collision.contactCount > 0)
            {
                ContactPoint2D contact = collision.GetContact(0);
                point = contact.point;
                normal = contact.normal;
            }
            HandleContact(collision.collider, point, normal);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || bodyCollider == null) return;
            ColliderDistance2D distance = bodyCollider.Distance(other);
            if (!distance.isValid || distance.distance > 0.05f) return;
            Vector2 normal = distance.normal.sqrMagnitude > 0.0001f ? -distance.normal : Vector2.up;
            HandleContact(other, distance.pointB, normal);
        }

        private void HandleContact(Collider2D other, Vector2 point, Vector2 normal)
        {
            if (exploded || IsHeld || other == null) return;
            if (other.transform.IsChildOf(transform)) return;
            if (Time.time < armedTime) return;
            MoonBoss hitBoss = other.GetComponentInParent<MoonBoss>();
            if (hitBoss == owner && !isThrown) return;
            Explode(point, normal);
        }

        private void Explode(Vector2 point, Vector2 normal)
        {
            if (exploded) return;
            exploded = true;
            Rigidbody.linearVelocity = Vector2.zero;
            Rigidbody.simulated = false;

            if (explosionCaster != null)
            {
                explosionCaster.transform.position = transform.position;
                explosionCaster.ConfigureCircle(explosionRadius, playerLayer);
                explosionCaster.Cast(new DamageData(playerDamage, DamageType.Special));
                if (isThrown && owner != null)
                {
                    LayerMask bossLayer = 1 << owner.gameObject.layer;
                    explosionCaster.ConfigureCircle(explosionRadius, bossLayer);
                    explosionCaster.Cast(new DamageData(bossDamage, DamageType.Special));
                }
            }

            if (owner != null)
            {
                Vector3 effectPoint = new Vector3(point.x, point.y, transform.position.z);
                owner.PlayRockImpactEffect(effectPoint, normal);
                owner.ShakeCamera(isThrown ? 0.78f : 0.62f);
                owner.PlayRockExplosionFeedback(transform.position);
            }
            Despawn();
        }

        private void Despawn()
        {
            grabInfoDisplay?.SetHeld(false);
            ODKPool.Despawn(this);
        }

        public void CancelBossSpawn()
        {
            if (exploded) return;
            exploded = true;
            explosionCaster?.DisableCasting();
            if (Rigidbody != null)
            {
                Rigidbody.linearVelocity = Vector2.zero;
                Rigidbody.angularVelocity = 0f;
            }
            ODKPool.Despawn(this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.45f, 0.15f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
            Gizmos.color = new Color(1f, 0.95f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, grabAssistRadius);
        }
    }
}
