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
        public enum EncounterPhase { PhaseOne, Transition, PhaseTwo }
        [Header("Encounter")]
        [SerializeField] private Transform target;
        [SerializeField] private bool playOnStart;
        [SerializeField, Min(0f)] private float patternInterval = 1f;
        [SerializeField] private UnityEvent onDeath;
        [SerializeField] private MimicBodyAnimator bodyAnimator;

        [Header("Half Health Boom")]
        [SerializeField] private ParticleSystem boomPrefab;
        [SerializeField] private Camera boomCamera;
        [SerializeField, Min(1)] private int boomCount = 5;
        [SerializeField, Min(0.01f)] private float boomInterval = 0.3f;
        [SerializeField, Min(0.01f)] private float boomScale = 1f;
        [SerializeField, Range(0f, 0.45f)] private float boomScreenPadding = 0.2f;
        private readonly List<ParticleSystem> _booms = new List<ParticleSystem>();
        private bool _halfHealthTriggered;
        private int _boomsRemaining;
        private float _boomTimer;

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
        [Tooltip("Skill IDs to repeat in phase one. Patterns are components on child objects.")]
        [SerializeField] private int[] phaseOneSkillIds = { 1, 2 };
        [SerializeField, Min(1)] private int tongueSkillId = 3;
        [SerializeField, Min(1)] private int coinRainSkillId = 4;
        [SerializeField] private int[] phaseTwoSkillIds = { 5, 6 };
        [SerializeField, Min(0f)] private float phaseTwoInterval = 5f;
        [SerializeField, Min(4)] private int maxUnheldWeapons = 32;
        private bool _coinRainStarted;
        private readonly Dictionary<int, MimicPattern> _patternsById = new Dictionary<int, MimicPattern>();

        private readonly List<MimicWeapon> _spawnedWeapons = new List<MimicWeapon>();
        private readonly List<MimicHazard> _spawnedHazards = new List<MimicHazard>();
        private float _idleTimer;
        private int _nextPattern;
        private bool _encounterActive;

        public MimicPatternRunner Patterns { get; } = new MimicPatternRunner();
        public MimicBodyAnimator BodyAnimator => bodyAnimator;
        public MimicTonguePattern Tongue => GetPattern(tongueSkillId) as MimicTonguePattern;
        public Transform Target => target;
        public MimicArena Arena => arena;
        public MimicHazard RockPrefab => rockPrefab;
        public Vector3 MouthPosition => mouth != null ? mouth.position : transform.position;
        public bool IsEncounterActive => _encounterActive;
        public EncounterPhase Phase { get; private set; }
        public bool HasWeaponPrefabs => weaponPrefabs != null && weaponPrefabs.Length > 0 &&
            Array.TrueForAll(weaponPrefabs, prefab => prefab != null);
        public bool CanEmitWeapons => mouth != null && landingLeft != null && landingRight != null &&
            HasPlatforms(firstPlatforms) && HasPlatforms(secondPlatforms) &&
            weaponPrefabs != null && weaponPrefabs.Length > 0 && Array.TrueForAll(weaponPrefabs, prefab => prefab != null);

        private static bool HasPlatforms(Collider2D[] platforms) => platforms != null && platforms.Length > 0 &&
            Array.TrueForAll(platforms, platform => platform != null && platform.enabled &&
                !platform.isTrigger && platform.gameObject.activeInHierarchy);

        protected override void AfterInitializeModules()
        {
            base.AfterInitializeModules();
            HealthModule.OnDeath += HandleDeath;
            HealthModule.OnHealthChanged += HandleHealthChanged;
        }

        public MimicPattern GetPattern(int skillId) =>
            _patternsById.TryGetValue(skillId, out MimicPattern pattern) ? pattern : null;

        public bool TryStartSkill(int skillId) => Phase != EncounterPhase.Transition && Patterns.Start(GetPattern(skillId));

        public bool InitializePatternDictionary()
        {
            _patternsById.Clear();
            foreach (MimicPattern pattern in GetComponentsInChildren<MimicPattern>(true))
            {
                if (pattern.GetComponentInParent<MimicBoss>() != this)
                    continue;
                if (pattern.SkillId <= 0 || !_patternsById.TryAdd(pattern.SkillId, pattern))
                {
                    Debug.LogError($"Mimic pattern '{pattern.name}' has an invalid or duplicate Skill ID: {pattern.SkillId}.", pattern);
                    _patternsById.Clear();
                    return false;
                }
                pattern.Initialize(this);
            }
            return true;
        }

        private void Start()
        {
            if (!playOnStart)
                InitializePatternDictionary();
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
            if (!InitializePatternDictionary())
                return;
            if (phaseOneSkillIds == null || phaseOneSkillIds.Length == 0 ||
                Array.Exists(phaseOneSkillIds, id => GetPattern(id) == null) || Tongue == null)
            {
                Debug.LogError("Mimic needs registered phase-one Skill IDs and a Tongue Skill ID pointing to MimicTonguePattern.", this);
                return;
            }
            if (target == null || !CanEmitWeapons || !Tongue.CanStart())
            {
                Debug.LogError("Mimic needs a target, mouth, weapon prefabs, ground landing bounds, both platform levels, tongue line and patterns.", this);
                return;
            }

            _nextPattern = 0;
            _idleTimer = 0f;
            _coinRainStarted = false;
            _halfHealthTriggered = false;
            Phase = EncounterPhase.PhaseOne;
            _encounterActive = true;
            HandleHealthChanged(HealthModule.CurrentHealth, HealthModule.MaxHealth);
        }

        private void Update()
        {
            UpdateBooms();
            if (!_encounterActive)
                return;
            if (target == null || HealthModule.IsDead)
            {
                StopEncounter();
                return;
            }

            if (Phase == EncounterPhase.Transition)
            {
                if (!_coinRainStarted && _boomsRemaining == 0 && _booms.Count == 0)
                {
                    _coinRainStarted = true;
                    if (!Patterns.Start(GetPattern(coinRainSkillId)))
                    {
                        Debug.LogError("Mimic CoinRain pattern is missing or not configured.", this);
                        StopEncounter();
                    }
                    return;
                }
                if (Patterns.IsRunning)
                    Patterns.Tick(Time.deltaTime);
                else if (_coinRainStarted && !_spawnedHazards.Exists(item => item != null && item.gameObject.activeInHierarchy))
                    EnterPhaseTwo();
                return;
            }

            TickPhase(Time.deltaTime);
        }

        private void EnterPhaseTwo()
        {
            if (phaseTwoSkillIds == null || phaseTwoSkillIds.Length == 0 ||
                Array.Exists(phaseTwoSkillIds, id => GetPattern(id) == null || !GetPattern(id).CanStart()))
            {
                Debug.LogError("Mimic needs configured phase-two patterns (FallingWeapons and Laser).", this);
                StopEncounter();
                return;
            }
            Phase = EncounterPhase.PhaseTwo;
            _nextPattern = 0;
            _idleTimer = 0f;
        }

        private void TickPhase(float deltaTime)
        {
            if (Patterns.IsRunning)
            {
                Patterns.Tick(deltaTime);
                if (!Patterns.IsRunning)
                    _idleTimer = Phase == EncounterPhase.PhaseTwo ? phaseTwoInterval : patternInterval;
                return;
            }

            _idleTimer -= deltaTime;
            if (_idleTimer > 0f)
                return;

            int[] skills = Phase == EncounterPhase.PhaseTwo ? phaseTwoSkillIds : phaseOneSkillIds;
            int skillId = skills[_nextPattern];
            if (TryStartSkill(skillId))
            {
                _nextPattern = (_nextPattern + 1) % skills.Length;
                return;
            }
            Debug.LogError($"Mimic cannot start skill {skillId}. Check its references.", this);
            StopEncounter();
        }

        public void TakeDamage(DamageData damage)
        {
            if (_encounterActive && damage.Amount > 0f)
                HealthModule.ApplyDamage(damage);
        }

        private void HandleHealthChanged(float current, float maximum)
        {
            if (!_encounterActive || _halfHealthTriggered || current <= 0f || maximum <= 0f || current > maximum * 0.5f)
                return;

            _halfHealthTriggered = true;
            Phase = EncounterPhase.Transition;
            Patterns.Cancel();
            bodyAnimator?.ResetPose();
            _boomsRemaining = Mathf.Max(1, boomCount);
            _boomTimer = 0f;
        }

        private void UpdateBooms()
        {
            for (int i = _booms.Count - 1; i >= 0; i--)
            {
                if (_booms[i] != null && _booms[i].IsAlive(true))
                    continue;
                if (_booms[i] != null)
                    Destroy(_booms[i].gameObject);
                _booms.RemoveAt(i);
            }

            if (_boomsRemaining <= 0)
                return;
            _boomTimer -= Time.deltaTime;
            if (_boomTimer > 0f)
                return;

            Camera view = boomCamera != null ? boomCamera : Camera.main;
            if (boomPrefab == null || view == null)
            {
                Debug.LogWarning("Mimic half-health Boom needs a particle prefab and camera.", this);
                _boomsRemaining = 0;
                return;
            }

            // Intersect the viewport ray with the boss's 2D gameplay plane.
            float padding = Mathf.Clamp(boomScreenPadding, 0f, 0.45f);
            Ray ray = view.ViewportPointToRay(new Vector3(
                UnityEngine.Random.Range(padding, 1f - padding),
                UnityEngine.Random.Range(padding, 1f - padding), 0f));
            Plane plane = new Plane(Vector3.forward, transform.position);
            if (plane.Raycast(ray, out float distance))
            {
                ParticleSystem effect = Instantiate(boomPrefab, ray.GetPoint(distance), boomPrefab.transform.rotation);
                effect.transform.localScale *= Mathf.Max(0.01f, boomScale);
                foreach (ParticleSystemRenderer renderer in effect.GetComponentsInChildren<ParticleSystemRenderer>(true))
                {
                    renderer.sortingLayerName = "Weapon";
                    renderer.sortingOrder = 100;
                }
                effect.Play(true);
                _booms.Add(effect);
            }
            _boomsRemaining--;
            _boomTimer = Mathf.Max(0.01f, boomInterval);
        }

        private void ClearBooms()
        {
            _boomsRemaining = 0;
            foreach (ParticleSystem effect in _booms)
                if (effect != null)
                    Destroy(effect.gameObject);
            _booms.Clear();
        }

        public MimicWeapon EmitTreasureWeapon(MimicTreasureHeight height, float arcHeight)
        {
            if (!CanEmitWeapons)
                return null;

            MimicWeapon prefab = weaponPrefabs[UnityEngine.Random.Range(0, weaponPrefabs.Length)];
            TrimWeapons();
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

        public MimicWeapon EmitFallingWeapon(Vector3 origin, Collider2D platform, float arcHeight)
        {
            if (!HasWeaponPrefabs || platform == null || !platform.enabled || Physics2D.gravity.y >= 0f)
                return null;
            TrimWeapons();
            MimicWeapon weapon = Instantiate(weaponPrefabs[UnityEngine.Random.Range(0, weaponPrefabs.Length)], origin, Quaternion.identity);
            Vector2 landing = SamplePlatformLanding(platform.bounds, weapon.GetComponent<Collider2D>().bounds.extents,
                platformEdgeInset, UnityEngine.Random.value);
            weapon.LaunchToSurfaceFromBoss(this, landing, arcHeight, true);
            _spawnedWeapons.Add(weapon);
            return weapon;
        }

        private void TrimWeapons()
        {
            _spawnedWeapons.RemoveAll(item => item == null || !item.gameObject.activeInHierarchy);
            int unheld = _spawnedWeapons.FindAll(item => !item.IsHeld).Count;
            for (int i = 0; i < _spawnedWeapons.Count && unheld >= Mathf.Max(4, maxUnheldWeapons);)
            {
                if (_spawnedWeapons[i].IsHeld) { i++; continue; }
                _spawnedWeapons[i].Retire();
                _spawnedWeapons.RemoveAt(i);
                unheld--;
            }
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
            ClearBooms();
            _encounterActive = false;
            Patterns.Cancel();
            bodyAnimator?.ResetPose();
            ClearWeapons();
        }

        private void HandleDeath()
        {
            ClearBooms();
            _encounterActive = false;
            Patterns.Cancel(true);
            bodyAnimator?.ResetPose();
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
            {
                HealthModule.OnDeath -= HandleDeath;
                HealthModule.OnHealthChanged -= HandleHealthChanged;
            }
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
