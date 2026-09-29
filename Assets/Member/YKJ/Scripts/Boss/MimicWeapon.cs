using System;
using System.Collections.Generic;
using Member.KYM.Scripts.Players.RobotArm;
using Member.KYM.Scripts.CombatSystems.DamageSystems;
using Member.ODK._01_Script;
using Member.ODK.Scripts;
using UnityEngine;
using UnityEngine.Serialization;

namespace Member.YKJ.Bosses
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class MimicWeapon : GrabbableRigidbody
    {
        public enum WeaponState { Grounded, BossFlight, Held, PlayerThrown, Spent }

        [SerializeField, Min(0f)] private float bossDamage = 80f;
        [SerializeField, Min(0f)] private float playerDamage = 10f;
        [SerializeField, Min(0.1f)] private float thrownLifetime = 8f;
        [SerializeField] private LayerMask groundLayers = (1 << 3) | (1 << 10);

        [Header("Appearance")]
        [SerializeField] private SpriteRenderer weaponRenderer;
        [SerializeField] private List<Sprite> weaponSprites = new List<Sprite>();
        [FormerlySerializedAs("maxVisualSize")]
        [Tooltip("Longest sprite dimension in weapon-local units; keeps different sprite import scales consistent.")]
        [SerializeField, Min(0.1f)] private float visualSize = 1f;
        [Header("Flight Trail")]
        [SerializeField] private Material trailMaterial;
        [SerializeField, Min(0.01f)] private float trailTime = 0.12f;
        [SerializeField, Min(0.01f)] private float trailWidth = 0.1f;
        private TrailRenderer _trail;
        private WeaponState _trailState = WeaponState.Spent;

        private readonly List<Collider2D> _ignoredColliders = new List<Collider2D>();
        private Collider2D _collider;
        private MimicBoss _boss;
        private Transform _throwOwner;
        private bool _hurtsPlayer;
        private bool _retireWhenReleased;
        private float _remainingLifetime;
        private Vector2 _launchPosition;
        private float _gravityBeforeGrab;

        public WeaponState State { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            _collider = GetComponent<Collider2D>();
            // Enable landing for this weapon only; leave the shared layer matrix unchanged.
            _collider.includeLayers |= groundLayers;
            int playerMask = LayerMask.GetMask("Player");
            _collider.includeLayers &= ~playerMask;
            _collider.excludeLayers |= playerMask;
            _collider.layerOverridePriority = Mathf.Max(1, _collider.layerOverridePriority);
            Rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _gravityBeforeGrab = Rigidbody.gravityScale;
            _launchPosition = transform.position;
            SetRandomSprite();
            FitAppearance();
            if (trailMaterial != null)
            {
                _trail = new GameObject("WeaponFlightTrail").AddComponent<TrailRenderer>();
                _trail.transform.SetParent(transform, false);
                _trail.sharedMaterial = trailMaterial;
                _trail.time = Mathf.Max(0.01f, trailTime);
                _trail.minVertexDistance = 0.08f;
                _trail.startWidth = trailWidth;
                _trail.endWidth = 0f;
                _trail.sortingLayerName = "Weapon";
                _trail.sortingOrder = 35;
                _trail.emitting = false;
            }
        }

        private void SetRandomSprite()
        {
            if (weaponSprites == null || weaponSprites.Count == 0)
                return;
            if (weaponRenderer == null)
                weaponRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (weaponRenderer == null)
                return;

            List<Sprite> available = weaponSprites.FindAll(sprite => sprite != null);
            if (available.Count > 0)
                weaponRenderer.sprite = available[UnityEngine.Random.Range(0, available.Count)];
        }

        private void FitAppearance()
        {
            if (weaponRenderer == null || weaponRenderer.sprite == null ||
                weaponRenderer.transform == transform || !weaponRenderer.transform.IsChildOf(transform))
                return;

            Transform visual = weaponRenderer.transform;
            Bounds spriteBounds = weaponRenderer.sprite.bounds;
            Vector3 right = transform.InverseTransformVector(visual.TransformVector(Vector3.right * spriteBounds.size.x));
            Vector3 up = transform.InverseTransformVector(visual.TransformVector(Vector3.up * spriteBounds.size.y));
            Vector2 size = new Vector2(Mathf.Abs(right.x) + Mathf.Abs(up.x), Mathf.Abs(right.y) + Mathf.Abs(up.y));
            float factor = Mathf.Max(0.1f, visualSize) / Mathf.Max(0.001f, Mathf.Max(size.x, size.y));
            visual.localScale *= factor;

            // Keep the physics root unchanged so throws and platform landing calculations remain valid.
            if (_collider is BoxCollider2D box)
            {
                box.offset = transform.InverseTransformPoint(visual.TransformPoint(spriteBounds.center));
                box.size = new Vector2(Mathf.Max(0.05f, size.x * factor), Mathf.Max(0.05f, size.y * factor));
            }
        }

        public void LaunchFromBoss(MimicBoss boss, Vector2 landing, float flightTime, bool hurtsPlayer)
        {
            Launch(boss, CalculateLaunchVelocity(transform.position, landing,
                Physics2D.gravity * Rigidbody.gravityScale, flightTime), hurtsPlayer);
        }

        public void LaunchToSurfaceFromBoss(MimicBoss boss, Vector2 landing, float arcHeight, bool hurtsPlayer)
        {
            Vector2 gravity = Physics2D.gravity * Rigidbody.gravityScale;
            Vector2 velocity = CalculateArcVelocity(transform.position, landing, gravity, arcHeight);
            // Rigidbody2D integrates gravity before position; compensate the half-step landing error.
            Launch(boss, velocity - gravity * (Time.fixedDeltaTime * 0.5f), hurtsPlayer);
        }

        private void Launch(MimicBoss boss, Vector2 velocity, bool hurtsPlayer)
        {
            _boss = boss;
            _hurtsPlayer = hurtsPlayer;
            _retireWhenReleased = false;
            _throwOwner = null;
            State = WeaponState.BossFlight;
            _launchPosition = transform.position;
            Rigidbody.bodyType = RigidbodyType2D.Dynamic;
            Rigidbody.linearDamping = 0f;
            Rigidbody.linearVelocity = velocity;
            IgnoreCollisionsWith(boss != null ? boss.transform : null);
            IgnoreCollisionsWith(boss != null ? boss.Target : null);
            RefreshTrail();
        }

        public static Vector2 CalculateLaunchVelocity(Vector2 origin, Vector2 target, Vector2 gravity, float seconds)
        {
            seconds = Mathf.Max(0.05f, seconds);
            return (target - origin) / seconds - gravity * (seconds * 0.5f);
        }

        public static Vector2 CalculateArcVelocity(Vector2 origin, Vector2 target, Vector2 gravity, float arcHeight)
        {
            if (gravity.y >= 0f)
                throw new ArgumentOutOfRangeException(nameof(gravity), "Treasure arcs require downward gravity.");

            float g = -gravity.y;
            float peak = Mathf.Max(origin.y, target.y) + Mathf.Max(0.05f, arcHeight);
            float riseTime = Mathf.Sqrt(2f * (peak - origin.y) / g);
            float fallTime = Mathf.Sqrt(2f * (peak - target.y) / g);
            float time = riseTime + fallTime;
            return CalculateLaunchVelocity(origin, target, gravity, time);
        }

        public override void Grab(Transform grabPoint, GameObject grabber)
        {
            if (CanBeGrabbed && State != WeaponState.PlayerThrown)
                _gravityBeforeGrab = Rigidbody.gravityScale;

            base.Grab(grabPoint, grabber);
        }

        protected override void OnGrabbed()
        {
            bool caughtInFlight = State == WeaponState.BossFlight;
            State = WeaponState.Held;
            _hurtsPlayer = false;
            _remainingLifetime = 0f;
            RefreshTrail();
            if (caughtInFlight && _boss != null && Grabber != null && !Grabber.transform.IsChildOf(_boss.transform))
                _boss.CombatVfx?.Catch(transform.position);
        }

        protected override void OnReleased()
        {
            State = WeaponState.Grounded;
            _hurtsPlayer = false;
            Rigidbody.gravityScale = _gravityBeforeGrab;
            RestoreIgnoredCollisions();
            IgnoreCollisionsWith(_boss != null ? _boss.Target : null);
            RefreshTrail();
            if (_retireWhenReleased)
                Retire();
        }

        protected override void OnThrown(ThrowData throwData)
        {
            State = WeaponState.PlayerThrown;
            _hurtsPlayer = false;
            Rigidbody.gravityScale = 0f;
            Rigidbody.linearDamping = 0f;
            _remainingLifetime = thrownLifetime;
            _throwOwner = throwData.Owner != null ? throwData.Owner.transform : null;
            RestoreIgnoredCollisions();
            IgnoreCollisionsWith(_throwOwner);
            IgnoreCollisionsWith(_boss != null ? _boss.Target : null);
            RefreshTrail();
            if (_retireWhenReleased)
                Retire();
        }

        private void Update()
        {
            RefreshTrail();
            if (IsHeld || State == WeaponState.Spent)
                return;

            if (State == WeaponState.PlayerThrown)
            {
                _remainingLifetime -= Time.deltaTime;
                if (_remainingLifetime <= 0f)
                    Retire();
            }

            if (Vector2.SqrMagnitude((Vector2)transform.position - _launchPosition) > 40000f)
                Retire();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleImpact(collision.collider);
            if (State != WeaponState.BossFlight ||
                (groundLayers.value & (1 << collision.collider.gameObject.layer)) == 0)
                return;

            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y <= 0.5f)
                    continue;
                State = WeaponState.Grounded;
                _hurtsPlayer = false;
                // The arena platforms have zero friction; leave landed treasure on their tops.
                Rigidbody.linearVelocity = Vector2.zero;
                Rigidbody.angularVelocity = 0f;
                RefreshTrail();
                break;
            }
        }
        private void OnTriggerEnter2D(Collider2D other) => HandleImpact(other);

        private void HandleImpact(Collider2D other)
        {
            if (IsHeld || State == WeaponState.Spent || other == null ||
                (LayerMask.GetMask("Player") & (1 << other.gameObject.layer)) != 0 ||
                (_boss != null && _boss.Target != null && other.transform.IsChildOf(_boss.Target)) ||
                other.transform.IsChildOf(transform) ||
                (_throwOwner != null && other.transform.IsChildOf(_throwOwner)))
                return;

            if (State == WeaponState.PlayerThrown)
            {
                if (other.GetComponentInParent<IGrabbable>() != null)
                    return;
                IDamageable receiver = other.GetComponentInParent<IDamageable>();
                if (receiver != null)
                {
                    // Disable first: damage can synchronously kill the boss and clear all weapons.
                    State = WeaponState.Spent;
                    _collider.enabled = false;
                    var damage = new DamageData(bossDamage, DamageType.Projectile);
                    Vector2 hitPoint = other.ClosestPoint(transform.position);
                    if (receiver is MimicBoss mimic)
                    {
                        float healthBefore = PlayerDamageFeedback.ReadHealth(mimic);
                        mimic.TakeWeaponDamage(damage, hitPoint);
                        PlayerDamageFeedback.Report(ThrowOwner, mimic, healthBefore, hitPoint);
                    }
                    else
                        PlayerDamageFeedback.Apply(ThrowOwner, receiver, damage, hitPoint);
                    Retire();
                }
                else if (!other.isTrigger)
                {
                    Retire();
                }
                return;
            }

            if (State != WeaponState.BossFlight)
                return;
            if (_boss != null && other.transform.IsChildOf(_boss.transform))
                return;

            if (_hurtsPlayer && _boss != null && _boss.Target != null &&
                other.transform.IsChildOf(_boss.Target))
            {
                other.GetComponentInParent<IDamageable>()?.TakeDamage(new DamageData(playerDamage, DamageType.Projectile));
                Retire();
                return;
            }
        }

        public void Retire()
        {
            // The arm owns its held reference. Leave it valid until Release/Throw detaches it.
            if (IsHeld)
            {
                _retireWhenReleased = true;
                return;
            }

            State = WeaponState.Spent;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private void IgnoreCollisionsWith(Transform owner)
        {
            if (owner == null)
                return;
            foreach (Collider2D other in owner.GetComponentsInChildren<Collider2D>())
            {
                if (other == _collider || Physics2D.GetIgnoreCollision(_collider, other))
                    continue;
                Physics2D.IgnoreCollision(_collider, other, true);
                _ignoredColliders.Add(other);
            }
        }

        private void RestoreIgnoredCollisions()
        {
            foreach (Collider2D other in _ignoredColliders)
            {
                if (other != null && _collider != null)
                    Physics2D.IgnoreCollision(_collider, other, false);
            }
            _ignoredColliders.Clear();
        }

        private void RefreshTrail()
        {
            if (_trail == null || _trailState == State) return;
            _trailState = State;
            _trail.Clear();
            _trail.emitting = State == WeaponState.BossFlight || State == WeaponState.PlayerThrown;
            Color color = State == WeaponState.PlayerThrown ? new Color(0.08f, 0.9f, 1f, 0.9f) :
                new Color(1f, 0.4f, 0.08f, 0.55f);
            _trail.startColor = color;
            color.a = 0f;
            _trail.endColor = color;
        }

        private void OnDisable()
        {
            RestoreIgnoredCollisions();
            if (_trail != null)
            {
                _trail.emitting = false;
                _trail.Clear();
            }
            _trailState = WeaponState.Spent;
        }
    }
}
