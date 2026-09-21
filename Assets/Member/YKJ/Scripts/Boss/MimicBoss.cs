using System;
using System.Collections.Generic;
using Member.KYM.Scripts.Agents;
using Member.ODK._01_Script;
using Member.ODK.Scripts;
using UnityEngine;
using UnityEngine.Events;

namespace Member.YKJ.Bosses
{
    [RequireComponent(typeof(HealthModule))]
    public sealed class MimicBoss : Agent, IDamageable
    {
        [Header("Encounter")]
        [SerializeField] private Transform target;
        [SerializeField] private bool playOnStart;
        [SerializeField, Min(0f)] private float patternInterval = 1f;
        [SerializeField] private UnityEvent onDeath;

        [Header("Treasure")]
        [SerializeField] private Transform mouth;
        [SerializeField] private MimicWeapon[] weaponPrefabs;
        [SerializeField] private Transform landingLeft;
        [SerializeField] private Transform landingRight;
        [Tooltip("Assign only the lower platform colliders. Each platform is a separate X range.")]
        [SerializeField] private Collider2D[] firstPlatforms;
        [Tooltip("Assign only the upper platform colliders.")]
        [SerializeField] private Collider2D[] secondPlatforms;
        [SerializeField, Min(0f)] private float platformEdgeInset = 0.65f;

        [Header("Arena / Rocks")]
        [SerializeField] private MimicArena arena;
        [SerializeField] private MimicHazard rockPrefab;

        [Header("Patterns")]
        [SerializeReference] private List<MimicPattern> phaseOnePatterns = new List<MimicPattern>
        {
            new MimicTreasurePattern(),
            new MimicJumpPattern()
        };
        [SerializeField] private MimicTonguePattern tongue = new MimicTonguePattern();

        private readonly List<MimicWeapon> _spawnedWeapons = new List<MimicWeapon>();
        private readonly List<MimicHazard> _spawnedHazards = new List<MimicHazard>();
        private float _idleTimer;
        private int _nextPattern;
        private bool _encounterActive;

        public MimicPatternRunner Patterns { get; } = new MimicPatternRunner();
        public MimicTonguePattern Tongue => tongue;
        public Transform Target => target;
        public MimicArena Arena => arena;
        public MimicHazard RockPrefab => rockPrefab;
        public Vector3 MouthPosition => mouth != null ? mouth.position : transform.position;
        public bool IsEncounterActive => _encounterActive;
        public bool CanEmitWeapons => mouth != null && landingLeft != null && landingRight != null &&
            HasPlatforms(firstPlatforms) && HasPlatforms(secondPlatforms) &&
            weaponPrefabs != null && weaponPrefabs.Length > 0 && Array.TrueForAll(weaponPrefabs, prefab => prefab != null);

        private static bool HasPlatforms(Collider2D[] platforms) => platforms != null && platforms.Length > 0 &&
            Array.TrueForAll(platforms, platform => platform != null && platform.enabled &&
                !platform.isTrigger && platform.gameObject.activeInHierarchy);

        protected override void AfterInitializeModules()
        {
            base.AfterInitializeModules();
            tongue.Initialize(this);
            foreach (MimicPattern pattern in phaseOnePatterns)
                pattern?.Initialize(this);
            HealthModule.OnDeath += HandleDeath;
        }

        private void Start()
        {
            if (playOnStart)
                BeginEncounter();
        }

        [ContextMenu("Begin Encounter")]
        public void BeginEncounter()
        {
            if (!Application.isPlaying || _encounterActive || HealthModule.IsDead)
                return;
            if (target == null)
                target = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (target == null || !CanEmitWeapons || !tongue.CanStart() || phaseOnePatterns.Count == 0)
            {
                Debug.LogError("Mimic needs a target, mouth, weapon prefabs, ground landing bounds, both platform levels, tongue line and patterns.", this);
                return;
            }

            _nextPattern = 0;
            _idleTimer = 0f;
            _encounterActive = true;
        }

        private void Update()
        {
            if (!_encounterActive)
                return;
            if (target == null || HealthModule.IsDead)
            {
                StopEncounter();
                return;
            }

            if (Patterns.IsRunning)
            {
                Patterns.Tick(Time.deltaTime);
                return;
            }

            _idleTimer -= Time.deltaTime;
            if (_idleTimer > 0f)
                return;

            _idleTimer = patternInterval;
            for (int i = 0; i < phaseOnePatterns.Count; i++)
            {
                MimicPattern pattern = phaseOnePatterns[_nextPattern];
                _nextPattern = (_nextPattern + 1) % phaseOnePatterns.Count;
                if (Patterns.Start(pattern))
                    return;
            }
        }

        public void TakeDamage(DamageData damage)
        {
            if (_encounterActive && damage.Amount > 0f)
                HealthModule.ApplyDamage(damage);
        }

        public MimicWeapon EmitTreasureWeapon(MimicTreasureHeight height, float arcHeight)
        {
            if (!CanEmitWeapons)
                return null;

            MimicWeapon prefab = weaponPrefabs[UnityEngine.Random.Range(0, weaponPrefabs.Length)];
            MimicWeapon weapon = Instantiate(prefab, MouthPosition, Quaternion.identity);
            Vector2 landing;
            if (height == MimicTreasureHeight.Ground)
            {
                // Ground markers specify the resting weapon-center height; only X is randomized.
                landing = new Vector2(Mathf.Lerp(landingLeft.position.x, landingRight.position.x,
                    UnityEngine.Random.value), landingLeft.position.y);
            }
            else
            {
                Collider2D[] platforms = height == MimicTreasureHeight.FirstPlatform ? firstPlatforms : secondPlatforms;
                Collider2D platform = platforms[UnityEngine.Random.Range(0, platforms.Length)];
                landing = SamplePlatformLanding(platform.bounds, weapon.GetComponent<Collider2D>().bounds.extents,
                    platformEdgeInset, UnityEngine.Random.value);
            }
            weapon.LaunchToSurfaceFromBoss(this, landing, arcHeight, false);
            _spawnedWeapons.RemoveAll(item => item == null);
            _spawnedWeapons.Add(weapon);
            return weapon;
        }

        public static Vector2 SamplePlatformLanding(Bounds surface, Vector2 weaponHalfSize, float edgeInset, float t)
        {
            // Avoid capsule end caps and keep the whole weapon inside the selected platform.
            float margin = Mathf.Min(surface.extents.x, Mathf.Max(edgeInset, surface.extents.y) + weaponHalfSize.x);
            float x = Mathf.Lerp(surface.min.x + margin, surface.max.x - margin, t);
            return new Vector2(x, surface.max.y + weaponHalfSize.y + 0.02f);
        }

        public MimicHazard SpawnHazard(MimicHazard prefab, Vector3 position, Vector2 velocity,
            float gravity, float damage, float lifetime)
        {
            if (prefab == null)
                return null;
            MimicHazard hazard = Instantiate(prefab, position, Quaternion.identity);
            hazard.Launch(this, velocity, gravity, damage, lifetime);
            _spawnedHazards.RemoveAll(item => item == null);
            _spawnedHazards.Add(hazard);
            return hazard;
        }

        [ContextMenu("Stop Encounter")]
        public void StopEncounter()
        {
            _encounterActive = false;
            Patterns.Cancel();
            ClearWeapons();
        }

        private void HandleDeath()
        {
            _encounterActive = false;
            Patterns.Cancel(true);
            ClearWeapons();
            onDeath?.Invoke();
        }

        private void ClearWeapons()
        {
            foreach (MimicWeapon weapon in _spawnedWeapons)
            {
                if (weapon != null)
                    weapon.Retire();
            }
            _spawnedWeapons.Clear();
            foreach (MimicHazard hazard in _spawnedHazards)
            {
                if (hazard != null)
                    hazard.Retire();
            }
            _spawnedHazards.Clear();
        }

        private void OnDisable() => StopEncounter();

        private void OnDestroy()
        {
            if (HealthModule != null)
                HealthModule.OnDeath -= HandleDeath;
        }

        private void OnDrawGizmosSelected()
        {
            if (landingLeft == null || landingRight == null)
                return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(landingLeft.position, landingRight.position);
            Gizmos.DrawWireSphere(MouthPosition, 0.2f);
        }
    }
}
