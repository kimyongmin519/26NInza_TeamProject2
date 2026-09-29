using Member.KYM.Scripts.CombatSystems.SkillSystems;
using Member.ODK.Scripts.Enemys.Skills;
using Member.ODK.Scripts.Enemys.Combat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Member.KYM.Scripts.UI;
using DG.Tweening;
using KimLIb.EventSystem;
using Member.KYM.Scripts.CoreSystems.Events;
using Member.KYM.Scripts.CoreSystems.Managers;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

namespace Member.ODK.Scripts.Enemys.Bosses
{
    public abstract class PhasedBossController : EnemyController
    {
        public event Action OnDefeated;
        [Header("Attack")]
        [SerializeField] private float attackInterval = 1f;
        [SerializeField] private bool playOnStart = true;

        [Header("Intro")]
        [SerializeField] private bool playIntro = true;

        [Header("Summon Presentation")]
        [SerializeField] private BossSummonPresentation summonPresentation;

        [Header("Death Presentation")]
        [SerializeField] private BossDeathPresentation deathPresentation;

        [Header("Target / Ground")]
        [SerializeField] private Transform target;
        [SerializeField] private LayerMask groundLayer = 1 << 3;
        [SerializeField] private BossArena arena;
        [SerializeField] private float groundRayHeight = 20f;
        [SerializeField] private float groundRayDistance = 50f;
        [SerializeField] private bool drawDebugGizmos = true;

        [Header("Camera Impulse")]
        [SerializeField] private CinemachineImpulseSource cameraImpulseSource;
        [SerializeField, Min(0f)] private float cameraShakeMinimumInterval = 0.07f;
        [SerializeField] private EventChannelSO cameraChannel;
        [SerializeField] private bool randomizeShakeDirection = true;

        [Header("Battle Camera")]
        [SerializeField] private bool switchToBattleCamera = true;
        [SerializeField] private string battleCameraName = "BattleCam";
        [SerializeField] private int battleCameraPriority = 20;

        [Header("Player Death")]
        [SerializeField] private bool stopOnPlayerDeath = true;
        [SerializeField] private EventChannelSO playerUIChannel;
        [SerializeField, Min(0f)] private float cameraShakePowerScale = 1f;
        [SerializeField, Min(0.01f)] private float cameraShakeBaseDuration = 0.14f;
        [SerializeField, Min(0f)] private float cameraShakeDurationPerPower = 0.16f;

        [Header("Health / Phase")]
        [SerializeField] private HealthModule healthModule;
        [SerializeField] private bool invincible;

        [Header("Health Bar")]
        [SerializeField] private BossHealthBarBinding healthBarBinding;

        [Header("Grab Rock")]
        [SerializeField] private GameObject[] rockVisualPrefabs;
        [SerializeField] private int fistRockCount = 1;
        [SerializeField] private int sawRockCount = 4;
        [SerializeField] private float rockSpawnSpread = 1.2f;
        [SerializeField] private float rockSpawnHeight = 0.15f;
        [SerializeField] private float rockLaunchForce = 5f;
        [SerializeField] private float rockScale = 1.3f;
        [SerializeField] private float rockDamage = DamageCaster.BossPlayerDamage;
        [SerializeField] private float rockLifeTime = 15f;

        [Header("Effect Hook")]
        [SerializeField] private BossPositionEvent onAttackReady;
        [SerializeField] private BossPositionEvent onAttackImpact;
        [SerializeField] private BossPositionEvent onMissileSpawn;
        [SerializeField] private BossPositionEvent onRockSpawn;
        [SerializeField] private BossPositionEvent onRockBreak;
        [SerializeField] private UnityEvent onPhaseTwo;
        [SerializeField] private UnityEvent onDeath;

        public Transform Target => target;
        public BossArena Arena => arena;
        public Vector3 ArenaCenter => arena != null ? arena.Center : (Vector3)originPos;
        public float ArenaHalfWidth => arena != null ? arena.HalfWidth : 13.5f;
        public float ArenaHalfHeight => arena != null ? arena.HalfHeight : 7.5f;
        public Vector2 originPos { get; private set; }
        public float CurrentHealth => healthModule != null ? healthModule.CurrentHealth : 0f;
        public float MaxHealth => healthModule != null ? healthModule.MaxHealth : 0f;
        public bool IsPhaseTwo { get; private set; }
        public bool IsDead { get; private set; }
        public bool PlayerDefeated { get; private set; }
        protected virtual bool HasPhaseTwo => true;
        protected virtual float PhaseTransitionDelay => 1.1f;
        private bool battleStarted;
        private float nextCameraShakeTime;
        private float lastCameraShakePower;
        private CinemachineImpulseSource shakeSource;
        private EnemySkillModule fallbackSkillModule;
        private float lastShakeSign = 1f;

        protected override void Awake()
        {
            base.Awake();
            originPos = transform.position;
            if (arena == null) arena = FindFirstObjectByType<BossArena>();
            if (cameraImpulseSource == null)
                cameraImpulseSource = GetComponent<CinemachineImpulseSource>();
            if (cameraImpulseSource == null)
                cameraImpulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
            if (summonPresentation == null)
                summonPresentation = GetComponent<BossSummonPresentation>();
            if (deathPresentation == null)
                deathPresentation = GetComponent<BossDeathPresentation>();
            if (healthBarBinding == null)
                healthBarBinding = GetComponent<BossHealthBarBinding>();
            if (healthBarBinding == null)
                healthBarBinding = gameObject.AddComponent<BossHealthBarBinding>();
            ResolveCameraChannel();
            ConfigureImpulseSource();
            if (cameraChannel == null) EnsureImpulseListener();
            if (healthModule == null) healthModule = GetModule<HealthModule>();
            if (healthModule != null) healthModule.OnDeath += HandleHealthDeath;
            SubscribePlayerDeath();
            if (playIntro && GetComponentInChildren<BossIntroTimeline>(true) == null)
                gameObject.AddComponent<BossIntroTimeline>();
        }

        protected virtual void Start()
        {
            FindTarget();
            InitializeAttacks();
            if (playOnStart) BeginBattle();
        }

        public bool BattleStarted => battleStarted;

        public void SetAutoStart(bool autoStart)
        {
            playOnStart = autoStart;
        }

        public void BeginBattle()
        {
            if (battleStarted || IsDead || PlayerDefeated) return;
            battleStarted = true;
            SwitchToBattleCamera();
            healthBarBinding?.TryBind();
            StartCoroutine(AttackLoop());
        }

        protected abstract IEnumerator PhaseOneLoop();
        protected abstract IEnumerator PhaseTwoLoop();

        protected virtual void OnPhaseTwoEntered() { }
        protected virtual void OnPlayerDefeated() { }

        public void SwitchToBattleCamera()
        {
            if (!switchToBattleCamera) return;
            CinemachineCamera battleCamera = null;
            CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (CinemachineCamera candidate in cameras)
            {
                if (candidate == null || candidate.gameObject.scene != gameObject.scene) continue;
                if (!string.IsNullOrEmpty(battleCameraName) && candidate.name == battleCameraName)
                {
                    battleCamera = candidate;
                    break;
                }
                if (battleCamera == null && candidate.GetComponent<CinemachineConfiner2D>() != null)
                    battleCamera = candidate;
            }
            if (battleCamera == null) return;
            if (!battleCamera.gameObject.activeSelf) battleCamera.gameObject.SetActive(true);
            if (!battleCamera.enabled) battleCamera.enabled = true;
            if (battleCamera.Target.TrackingTarget == null)
            {
                Transform player = target != null ? target : FindPlayerTransform();
                if (player != null) battleCamera.Target.TrackingTarget = player;
            }
            int highest = battleCameraPriority;
            foreach (CinemachineCamera other in cameras)
            {
                if (other == null || other == battleCamera || !other.isActiveAndEnabled) continue;
                if (other.Priority.Enabled && other.Priority.Value >= highest && other.Priority.Value < 400)
                    highest = other.Priority.Value + 1;
            }
            battleCamera.Priority.Enabled = true;
            battleCamera.Priority.Value = highest;
            battleCamera.Prioritize();
        }

        private void SubscribePlayerDeath()
        {
            if (!stopOnPlayerDeath) return;
            if (playerUIChannel == null) playerUIChannel = FindPlayerUIChannel();
            if (playerUIChannel == null) return;
            playerUIChannel.RemoveListener<PlayerUIStateEvent>(HandlePlayerState);
            playerUIChannel.AddListener<PlayerUIStateEvent>(HandlePlayerState);
        }

        private static EventChannelSO FindPlayerUIChannel()
        {
            Type[] owners = { typeof(GameOverUI), typeof(PlayerHealthUI), typeof(HeldProjectileUI) };
            foreach (Type ownerType in owners)
            {
                UnityEngine.Object owner = FindFirstObjectByType(ownerType, FindObjectsInactive.Include);
                if (owner == null) continue;
                FieldInfo field = ownerType.GetField("uiChannel", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field?.GetValue(owner) is EventChannelSO channel) return channel;
            }
            return null;
        }

        private void HandlePlayerState(PlayerUIStateEvent evt)
        {
            if (evt != null && evt.IsDead) HaltForPlayerDeath();
        }

        [ContextMenu("Halt For Player Death (Test)")]
        public void HaltForPlayerDeath()
        {
            if (PlayerDefeated || IsDead) return;
            PlayerDefeated = true;
            StopAllCoroutines();
            CancelAttacks();
            if (TryGetComponent(out Rigidbody2D body))
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
            SafeInvoke(OnPlayerDefeated);
        }
        protected virtual void OnBossDeath() { }
        protected virtual void OnAttackReady(Vector3 position) { }
        protected virtual void OnAttackImpact(Vector3 position) { }
        protected virtual void OnMissileSpawn(Vector3 position) { }

        private void InitializeAttacks()
        {
            ISkill[] skills = GetBossSkillModule()?.GetAllSkill();
            if (skills == null) return;

            foreach (ISkill skill in skills)
            {
                if (skill is ODKBossSkill attack)
                    attack.InitializeAttack(this);
            }
        }

        protected ODKBossSkill GetAttack(int index)
        {
            EnemySkillModule skillModule = GetBossSkillModule();
            if (skillModule == null)
            {
                Debug.LogError($"[{name}] {nameof(EnemySkillModule)} is missing.", this);
                return null;
            }

            ODKBossSkill attack = skillModule.GetSkill(index) as ODKBossSkill;
            if (attack == null)
            {
                Debug.LogError(
                    $"[{name}] Boss attack index {index} is missing or invalid.",
                    this
                );
                return null;
            }

            return attack;
        }

        private EnemySkillModule GetBossSkillModule()
        {
            if (SkillModule is EnemySkillModule module) return module;
            if (fallbackSkillModule == null)
            {
                fallbackSkillModule = GetComponentInChildren<EnemySkillModule>(true);
                if (fallbackSkillModule != null && fallbackSkillModule.GetAllSkill().Length == 0)
                    fallbackSkillModule.Initialize(this);
            }
            return fallbackSkillModule;
        }

        private IEnumerator AttackLoop()
        {
            yield return new WaitUntil(FindTarget);
            if (summonPresentation != null)
                yield return summonPresentation.Play(this);
            yield return AttackWait();

            while (!IsDead)
            {
                if (IsPhaseTwo) yield return PhaseTwoLoop();
                else yield return PhaseOneLoop();
            }
        }

        protected IEnumerator PlayAttack(int index, float durationScale = 1f)
        {
            ODKBossSkill attack = GetAttack(index);
            if (attack == null) yield break;

            yield return attack.Play(durationScale);
        }

        protected IEnumerator RunParallel(
            int firstIndex,
            float firstScale,
            int secondIndex,
            float secondScale)
        {
            int runningCount = 2;

            StartCoroutine(RunAttack(firstIndex, firstScale, () => runningCount--));
            StartCoroutine(RunAttack(secondIndex, secondScale, () => runningCount--));

            yield return new WaitUntil(() => runningCount <= 0 || IsDead);
        }

        private IEnumerator RunAttack(int index, float durationScale, Action onComplete)
        {
            ODKBossSkill attack = GetAttack(index);

            if (attack != null)
                yield return attack.Play(durationScale);

            onComplete?.Invoke();
        }

        protected WaitForSeconds AttackWait()
        {
            return new WaitForSeconds(attackInterval);
        }

        protected bool FindTarget()
        {
            if (target != null && target.gameObject.activeInHierarchy) return true;
            target = FindPlayerTransform();
            return target != null;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public static Transform FindPlayerTransform()
        {
            GameObject tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null) return tagged.transform;

            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer < 0) return null;
            Transform fallback = null;
            foreach (Rigidbody2D body in FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None))
            {
                if (body == null || body.gameObject.layer != playerLayer || !body.gameObject.activeInHierarchy) continue;
                if (body.bodyType == RigidbodyType2D.Dynamic) return body.transform;
                if (fallback == null) fallback = body.transform;
            }
            if (fallback != null) return fallback;
            foreach (Collider2D collider in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
            {
                if (collider != null && collider.gameObject.layer == playerLayer && collider.gameObject.activeInHierarchy)
                    return collider.attachedRigidbody != null ? collider.attachedRigidbody.transform : collider.transform;
            }
            return null;
        }

        public Vector3 GetGroundPoint(float x)
        {
            float targetY = target != null ? target.position.y : ArenaCenter.y;
            float startY = Mathf.Max(targetY, ArenaCenter.y) + groundRayHeight;
            RaycastHit2D hit = Physics2D.Raycast(
                new Vector2(x, startY),
                Vector2.down,
                groundRayDistance,
                groundLayer
            );
            return hit.collider != null
                ? (Vector3)hit.point
                : new Vector3(x, ArenaCenter.y, 0f);
        }

        public Vector3 GetImpactVisualPosition(
            Transform piece,
            Vector3 groundPoint,
            Vector2 visualOffset)
        {
            Vector3 impactPosition = groundPoint + (Vector3)visualOffset;
            if (piece == null) return impactPosition;

            impactPosition.y += GetGroundOffset(piece);
            impactPosition.z = piece.position.z;
            return impactPosition;
        }

        protected static float GetGroundOffset(Transform piece)
        {
            if (piece == null) return 0f;

            Collider2D pieceCollider = piece.GetComponentInChildren<Collider2D>();
            if (pieceCollider != null)
                return Mathf.Max(0f, piece.position.y - pieceCollider.bounds.min.y);

            SpriteRenderer pieceRenderer = piece.GetComponentInChildren<SpriteRenderer>();
            return pieceRenderer != null
                ? Mathf.Max(0f, piece.position.y - pieceRenderer.bounds.min.y)
                : 0f;
        }

        public void AttackReady(Vector3 position)
        {
            onAttackReady?.Invoke(position);
            OnAttackReady(position);
        }

        public void AttackImpact(Vector3 position)
        {
            onAttackImpact?.Invoke(position);
            OnAttackImpact(position);
        }

        public void MissileSpawn(Vector3 position)
        {
            onMissileSpawn?.Invoke(position);
            OnMissileSpawn(position);
        }

        public void RegisterSpawn(ICancellableBossSpawn spawn)
        {
        }

        public void CancelRegisteredSpawns()
        {
        }

        public void ShakeCameraFor(float power, float duration)
        {
            if (power <= 0f || duration <= 0f) return;
            lastCameraShakePower = power;
            nextCameraShakeTime = Time.time + cameraShakeMinimumInterval;
            EmitShake(power, duration);
        }

        public void ShakeCamera(float power)
        {
            if (power <= 0f) return;
            if (Time.time < nextCameraShakeTime && power <= lastCameraShakePower) return;
            lastCameraShakePower = power;
            nextCameraShakeTime = Time.time + cameraShakeMinimumInterval;
            EmitShake(power, cameraShakeBaseDuration + power * cameraShakeDurationPerPower);
        }

        private void EmitShake(float power, float duration)
        {
            if (randomizeShakeDirection && TryEmitDirectionalShake(power * cameraShakePowerScale, duration)) return;
            if (cameraChannel == null) ResolveCameraChannel();
            if (cameraChannel != null)
            {
                cameraChannel.RaiseEvent(new CameraShakeEvent().InitData(power * cameraShakePowerScale, duration));
                return;
            }
            if (cameraImpulseSource != null) cameraImpulseSource.GenerateImpulse(power);
        }

        private bool TryEmitDirectionalShake(float power, float duration)
        {
            if (power <= 0f || duration <= 0f) return false;
            if (shakeSource == null)
            {
                CameraShakeManager manager = FindFirstObjectByType<CameraShakeManager>(FindObjectsInactive.Include);
                if (manager != null) shakeSource = manager.GetComponent<CinemachineImpulseSource>();
                if (shakeSource == null) shakeSource = cameraImpulseSource;
            }
            if (shakeSource == null || shakeSource.ImpulseDefinition == null) return false;

            CinemachineImpulseDefinition source = shakeSource.ImpulseDefinition;
            CinemachineImpulseDefinition impulse = new CinemachineImpulseDefinition
            {
                ImpulseChannel = source.ImpulseChannel,
                ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Explosion,
                ImpulseDuration = duration,
                ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform,
                DissipationRate = source.DissipationRate,
                ImpactRadius = source.ImpactRadius,
                DirectionMode = source.DirectionMode,
                DissipationMode = source.DissipationMode,
                DissipationDistance = source.DissipationDistance,
                PropagationSpeed = source.PropagationSpeed
            };

            float magnitude = shakeSource.DefaultVelocity.magnitude;
            if (magnitude <= 0.0001f) magnitude = 1f;
            lastShakeSign = -lastShakeSign;
            Vector2 direction = new Vector2(lastShakeSign, UnityEngine.Random.Range(-0.3f, 0.3f)).normalized;
            impulse.CreateAndReturnEvent(transform.position, (Vector3)direction * magnitude * power);
            return true;
        }

        private void ResolveCameraChannel()
        {
            if (cameraChannel != null) return;
            CameraShakeManager manager = FindFirstObjectByType<CameraShakeManager>(FindObjectsInactive.Include);
            if (manager != null) cameraChannel = manager.cameraChannel;
        }

        private static void EnsureImpulseListener()
        {
            CinemachineCamera camera = FindFirstObjectByType<CinemachineCamera>();
            if (camera == null) return;

            CinemachineImpulseListener listener = camera.GetComponent<CinemachineImpulseListener>();
            if (listener == null) listener = camera.gameObject.AddComponent<CinemachineImpulseListener>();
            listener.ApplyAfter = CinemachineCore.Stage.Noise;
            listener.ChannelMask = 1;
            listener.Gain = 1f;
            listener.Use2DDistance = true;
            listener.UseCameraSpace = true;
        }

        private void ConfigureImpulseSource()
        {
            if (cameraImpulseSource == null) return;
            if (cameraImpulseSource.ImpulseDefinition == null)
                cameraImpulseSource.ImpulseDefinition = new CinemachineImpulseDefinition();

            CinemachineImpulseDefinition definition = cameraImpulseSource.ImpulseDefinition;
            definition.ImpulseChannel = 1;
            definition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Explosion;
            definition.ImpulseDuration = 0.2f;
            definition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            cameraImpulseSource.DefaultVelocity = Vector3.down;
        }

        public void SpawnFistRocks(Vector3 position) => SpawnRocks(position, fistRockCount);
        public void SpawnSawRocks(Vector3 position) => SpawnRocks(position, sawRockCount);

        private void SpawnRocks(Vector3 groundPoint, int count)
        {
            if (rockVisualPrefabs == null || rockVisualPrefabs.Length == 0 || count <= 0)
                return;

            for (int i = 0; i < count; i++)
            {
                GameObject visualPrefab = rockVisualPrefabs[UnityEngine.Random.Range(0, rockVisualPrefabs.Length)];
                if (visualPrefab == null) continue;

                float centerRate = count <= 1 ? 0f : (float)i / (count - 1) - 0.5f;
                Vector3 spawnPosition = groundPoint + Vector3.up * rockSpawnHeight;
                GameObject rockObject = ODKPool.Spawn(
                    visualPrefab,
                    spawnPosition,
                    Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-25f, 25f))
                );
                if (rockObject == null) continue;
                rockObject.name = name + " Rock";
                int propLayer = LayerMask.NameToLayer("Prop");
                if (propLayer >= 0) rockObject.layer = propLayer;
                rockObject.transform.localScale *= rockScale;

                if (!rockObject.TryGetComponent(out Rigidbody2D rockRigidbody))
                    rockRigidbody = rockObject.AddComponent<Rigidbody2D>();
                rockRigidbody.mass = 0.7f;
                rockRigidbody.gravityScale = 2f;
                rockRigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                if (!rockObject.TryGetComponent(out Collider2D _))
                {
                    CircleCollider2D rockCollider = rockObject.AddComponent<CircleCollider2D>();
                    SpriteRenderer rockRenderer = rockObject.GetComponent<SpriteRenderer>();
                    if (rockRenderer != null && rockRenderer.sprite != null)
                    {
                        rockCollider.radius = Mathf.Max(
                            rockRenderer.sprite.bounds.extents.x,
                            rockRenderer.sprite.bounds.extents.y
                        ) * 0.75f;
                        rockRenderer.sortingOrder = 15;
                    }
                }

                // The supplied point is the ground contact point.  Keep the spawned
                // collider completely above it so the physics solver cannot pin the
                // rock under the floor before its first upward step.
                Collider2D rockCollider2D = rockObject.GetComponent<Collider2D>();
                if (rockCollider2D != null)
                {
                    float bottomOffset = rockObject.transform.position.y - rockCollider2D.bounds.min.y;
                    spawnPosition.y = Mathf.Max(
                        spawnPosition.y,
                        groundPoint.y + bottomOffset + 0.08f
                    );
                    rockObject.transform.position = spawnPosition;
                }

                Vector2 launchDirection = new Vector2(
                    centerRate * rockSpawnSpread + UnityEngine.Random.Range(-0.25f, 0.25f),
                    1.2f
                ).normalized;
                Vector2 launchVelocity = launchDirection * UnityEngine.Random.Range(
                    rockLaunchForce * 0.75f,
                    rockLaunchForce * 1.25f
                );

                ConfigureRock(
                    rockObject,
                    rockRigidbody,
                    launchVelocity,
                    UnityEngine.Random.Range(-240f, 240f),
                    rockDamage,
                    rockLifeTime
                );
                onRockSpawn?.Invoke(spawnPosition);
            }
        }

        protected virtual void ConfigureRock(
            GameObject rockObject,
            Rigidbody2D rockRigidbody,
            Vector2 launchVelocity,
            float angularVelocity,
            float damage,
            float lifeTime)
        {
            rockRigidbody.linearVelocity = launchVelocity;
            rockRigidbody.angularVelocity = angularVelocity;
            Destroy(rockObject, lifeTime);
        }

        protected void RockBreak(Vector3 position)
        {
            onRockBreak?.Invoke(position);
        }

        protected void ApplyBossDamage(DamageData damage)
        {
            if (IsDead || invincible || healthModule == null) return;
            healthModule.ApplyDamage(damage);
        }

        private void HandleHealthDeath()
        {
            if (IsDead) return;
            if (!HasPhaseTwo || IsPhaseTwo)
            {
                Die();
                return;
            }

            EnterPhaseTwo();
            if (healthModule != null)
                healthModule.Revive();
        }

        [ContextMenu("Enter Phase Two")]
        public void EnterPhaseTwo()
        {
            if (IsPhaseTwo || IsDead || PlayerDefeated) return;

            IsPhaseTwo = true;
            StopAllCoroutines();
            CancelAttacks();
            SafeInvoke(OnPhaseTwoEntered);
            SafeInvoke(() => onPhaseTwo?.Invoke());
            StartCoroutine(PhaseTwoRestart());
        }

        private void SafeInvoke(Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }

        private IEnumerator PhaseTwoRestart()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, PhaseTransitionDelay));
            while (!IsDead && IsPhaseTwo)
                yield return PhaseTwoLoop();
        }

        protected void CancelAttacks()
        {
            summonPresentation?.Cancel();
            ISkill[] skills = GetBossSkillModule()?.GetAllSkill();
            if (skills != null)
            {
                foreach (ISkill skill in skills)
                {
                    if (skill is ODKBossSkill attack)
                        attack.StopSkill();
                }
            }

            transform.DOKill();
            DamageCaster[] casters = GetComponentsInChildren<DamageCaster>(true);
            foreach (DamageCaster caster in casters)
            {
                if (caster == null) continue;
                caster.DisableCasting();
                caster.ClearWorldPose();
            }
        }

        protected void Die()
        {
            if (IsDead) return;

            IsDead = true;
            SafeInvoke(OnDefeated);
            StopAllCoroutines();
            CancelAttacks();
            SafeInvoke(OnBossDeath);
            SafeInvoke(() => onDeath?.Invoke());
            if (deathPresentation != null)
                StartCoroutine(deathPresentation.Play(this));
        }

        protected virtual void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos) return;

            Vector3 center = arena != null ? arena.Center : transform.position;

            Gizmos.color = new Color(1f, 0.9f, 0.1f, 0.75f);
            for (int i = 0; i < 5; i++)
            {
                float rate = i / 4f;
                float x = Mathf.Lerp(
                    center.x - ArenaHalfWidth * 0.75f,
                    center.x + ArenaHalfWidth * 0.75f,
                    rate
                );
                float startY = center.y + groundRayHeight;
                RaycastHit2D hit = Physics2D.Raycast(
                    new Vector2(x, startY),
                    Vector2.down,
                    groundRayDistance,
                    groundLayer
                );
                Vector3 end = hit.collider != null
                    ? (Vector3)hit.point
                    : new Vector3(x, startY - groundRayDistance, 0f);
                Gizmos.DrawLine(new Vector3(x, startY, 0f), end);
                Gizmos.DrawWireSphere(end, 0.22f);
            }

            if (target != null)
            {
                Vector3 groundPoint = GetGroundPoint(target.position.x);
                Gizmos.color = new Color(1f, 0.45f, 0f, 0.85f);
                Gizmos.DrawWireSphere(groundPoint, rockSpawnSpread);
                Gizmos.DrawLine(groundPoint, groundPoint + Vector3.up * rockSpawnHeight);
            }
        }

        protected virtual void OnDestroy()
        {
            if (playerUIChannel != null) playerUIChannel.RemoveListener<PlayerUIStateEvent>(HandlePlayerState);
            if (healthModule != null) healthModule.OnDeath -= HandleHealthDeath;
            deathPresentation?.Cancel();
            CancelAttacks();
        }
    }
}
