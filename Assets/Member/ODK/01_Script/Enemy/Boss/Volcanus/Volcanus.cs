using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Member.ODK._01_Script;
using Member.ODK.Scripts.Enemys.Bosses;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public class Volcanus : PhasedBossController, IDamageable
    {
        public const string IdleState = "idle_1";
        public const string RoarState = "idle_2";
        public const string WalkState = "walk";
        public const string RunState = "run";
        public const string ComboState = "skill_1";
        public const string SlamState = "skill_2";
        public const string HitState = "hit_1";
        public const string DeathState = "death";

        [Header("Golem Visual")]
        [SerializeField] private Transform visualMotionRoot;
        [SerializeField] private Transform golemModelRoot;
        [SerializeField] private Vector3 visualLocalPosition;
        [SerializeField] private Vector3 visualLocalScale = Vector3.one;
        [SerializeField, Min(0.1f)] private float golemScale = 1.6f;
        [SerializeField] private bool fitVisualToHeight = true;
        [SerializeField, Min(1f)] private float targetVisualHeight = 8.6f;
        [SerializeField] private float visualCenterOffsetX;
        [SerializeField] private float visualGroundOffset = 0.08f;
        [SerializeField] private bool prefabFacesRight = true;
        [SerializeField] private int visualSortingOrder = 20;

        [Header("Golem Motion")]
        [SerializeField] private float motionResetDuration = 0.16f;
        [SerializeField] private Ease motionEase = Ease.OutCubic;
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float runSpeed = 8f;
        [SerializeField] private float phaseTwoAnimationSpeed = 1.15f;

        [Header("Phase Two Saw + Punch")]
        [SerializeField] private float punchStartSpeed = 1.3f;
        [SerializeField] private float punchSpeedStep = 0.45f;
        [SerializeField] private float punchMaxSpeed = 2.6f;
        [SerializeField, Min(1)] private int punchMaxCount = 4;

        [Header("Body / Head Facing")]
        [SerializeField] private Transform bodyFacingRoot;
        [SerializeField] private Transform headFacingRoot;
        [SerializeField] private bool faceTarget = true;

        [Header("Hitbox")]
        [SerializeField] private Vector2 hitboxSize = new Vector2(3.2f, 5.2f);
        [SerializeField] private Vector2 hitboxOffset = new Vector2(0f, 2.6f);
        [SerializeField] private LayerMask playerLayer = 1 << 6;
        [SerializeField] private LayerMask hazardGroundLayer = 1 << 3;

        [Header("Boulder")]
        [SerializeField] private GolemBoulder boulderPrefab;
        [SerializeField] private GolemGroundWave groundWavePrefab;
        [SerializeField] private LineRenderer telegraphLinePrefab;
        [SerializeField] private float boulderBossDamage = 1f;
        [SerializeField] private float boulderPlayerDamage = 30f;
        [SerializeField] private Vector2 boulderScaleRange = new Vector2(0.7f, 1.45f);

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private VolcanusGolemMotion golemMotion;
        [SerializeField] private string idleState = IdleState;
        [SerializeField] private string phaseTwoState = RoarState;
        [SerializeField] private string hitState = HitState;
        [SerializeField] private string deathState = DeathState;
        [SerializeField] private float phaseTransitionDuration = 1f;
        [SerializeField] private float animationFade = 0.06f;

        [Header("Feedback")]
        [SerializeField] private VolcanusFeedback feedback;
        [SerializeField] private CinemachineImpulseSource impulseSource;

        public LayerMask PlayerLayer => playerLayer;
        public LayerMask GroundLayer => hazardGroundLayer;
        public Animator Animator => animator;
        public float SizeScale => fitVisualToHeight
            ? targetVisualHeight / 5.4f
            : golemScale;
        public bool IsActing => actingCount > 0;
        public float ActionSpeed => IsPhaseTwo ? phaseTwoAnimationSpeed : 1f;
        public float WalkSpeed => walkSpeed * golemScale;
        public float RunSpeed => runSpeed * golemScale;
        protected override float PhaseTransitionDelay => phaseTransitionDuration;

        private Transform visualRoot;
        private Vector3 bodyFacingScale;
        private Vector3 headFacingScale;
        private string currentState;
        private int actingCount;
        private readonly Dictionary<string, AnimationClip> clipCache = new Dictionary<string, AnimationClip>();

        private const int RightPunchIndex = 0;
        private const int LeftPunchIndex = 1;
        private const int SawSlamIndex = 2;
        private const int SideSlashIndex = 3;
        private const int FiveSlamIndex = 4;
        private const int LaserIndex = 5;
        private const int MissileIndex = 6;

        protected override void Awake()
        {
            base.Awake();
            DisableLegacyPresentation();
            CreateGolemVisual();
            ConfigureHitbox();
            if (feedback == null) feedback = GetComponent<VolcanusFeedback>();
            if (feedback == null) feedback = gameObject.AddComponent<VolcanusFeedback>();
            if (impulseSource == null) impulseSource = GetComponent<CinemachineImpulseSource>();
            if (impulseSource == null) impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
            SnapToGround();
        }

        protected override IEnumerator PhaseOneLoop()
        {
            yield return PlayAttack(RightPunchIndex);
            yield return AttackWait();
            yield return PlayAttack(SawSlamIndex);
            yield return AttackWait();
            yield return PlayAttack(LeftPunchIndex);
            yield return AttackWait();
            yield return PlayAttack(SideSlashIndex);
            yield return AttackWait();
            yield return PlayAttack(FiveSlamIndex);
            yield return AttackWait();
            yield return PlayAttack(LaserIndex);
            yield return AttackWait();
        }

        protected override IEnumerator PhaseTwoLoop()
        {
            yield return RunParallel(SawSlamIndex, 1f, FiveSlamIndex, 1.5f);
            yield return AttackWait();
            yield return PlayAttack(MissileIndex);
            yield return AttackWait();
            yield return SawSlamWithAcceleratingPunch();
            yield return AttackWait();
            yield return PlayAttack(SideSlashIndex);
            yield return AttackWait();
            yield return PlayAttack(LaserIndex);
            yield return AttackWait();
        }

        private IEnumerator SawSlamWithAcceleratingPunch()
        {
            bool slamRunning = true;
            StartCoroutine(RunSawSlam(() => slamRunning = false));

            float speed = punchStartSpeed;
            int count = 0;
            while (slamRunning && !IsDead && count < punchMaxCount)
            {
                yield return PlayAttack(RightPunchIndex, speed);
                speed = Mathf.Min(punchMaxSpeed, speed + punchSpeedStep);
                count++;
            }

            while (slamRunning && !IsDead) yield return null;
        }

        private IEnumerator RunSawSlam(System.Action onComplete)
        {
            yield return PlayAttack(SawSlamIndex);
            onComplete?.Invoke();
        }

        private void LateUpdate()
        {
            if (!faceTarget || Target == null || bodyFacingRoot == null || IsDead) return;
            bool targetIsRight = Target.position.x >= transform.position.x;
            SetFacing(targetIsRight ? 1f : -1f);
        }

        public void SetFacing(float worldDirection)
        {
            if (bodyFacingRoot == null) return;
            bool right = worldDirection >= 0f;
            float direction = right == prefabFacesRight ? 1f : -1f;
            SetFacingScale(bodyFacingRoot, bodyFacingScale, direction);
            if (headFacingRoot != null && !headFacingRoot.IsChildOf(bodyFacingRoot))
                SetFacingScale(headFacingRoot, headFacingScale, direction);
        }

        private static void SetFacingScale(Transform target, Vector3 originScale, float direction)
        {
            Vector3 scale = originScale;
            scale.x = Mathf.Abs(originScale.x) * direction;
            target.localScale = scale;
        }

        private void DisableLegacyPresentation()
        {
            foreach (Renderer oldRenderer in GetComponentsInChildren<Renderer>(true))
            {
                if (oldRenderer == null) continue;
                if (visualMotionRoot != null && oldRenderer.transform.IsChildOf(visualMotionRoot))
                    continue;
                if (golemModelRoot != null && oldRenderer.transform.IsChildOf(golemModelRoot))
                    continue;
                oldRenderer.enabled = false;
            }
            foreach (Collider2D oldCollider in GetComponentsInChildren<Collider2D>(true))
            {
                if (oldCollider == null) continue;
                oldCollider.enabled = false;
            }
            foreach (MonoBehaviour behaviour in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null) continue;
                string behaviourNamespace = behaviour.GetType().Namespace;
                if (!string.IsNullOrEmpty(behaviourNamespace) &&
                    behaviourNamespace.Contains(".Volcanus.Legacy"))
                    behaviour.enabled = false;
            }
        }

        private void CreateGolemVisual()
        {
            if (visualRoot != null) return;
            if (golemModelRoot == null)
            {
                Debug.LogError("[Volcanus] Scene child Boss Golem Visual is not connected.", this);
                return;
            }

            visualRoot = visualMotionRoot != null ? visualMotionRoot : golemModelRoot;
            visualRoot.localPosition = visualLocalPosition;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one;

            GameObject instance = golemModelRoot.gameObject;
            instance.SetActive(true);
            golemModelRoot.localPosition = Vector3.zero;
            golemModelRoot.localRotation = Quaternion.identity;
            golemModelRoot.localScale = Vector3.Scale(golemModelRoot.localScale, visualLocalScale) * golemScale;
            headFacingRoot = FindNamedTransform(golemModelRoot, "head_1") ??
                FindNamedTransform(golemModelRoot, "head_2") ??
                FindNamedTransform(golemModelRoot, "head_3");

            animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.Update(0f);
                if (animator.GetComponent<GolemAnimationRelay>() == null)
                    animator.gameObject.AddComponent<GolemAnimationRelay>();
            }
            else
            {
                Debug.LogError("[Volcanus] Boss Golem visual has no Animator.", instance);
            }

            SortingGroup sortingGroup = instance.GetComponentInChildren<SortingGroup>(true);
            if (sortingGroup != null) sortingGroup.sortingOrder = visualSortingOrder;

            foreach (Collider2D visualCollider in instance.GetComponentsInChildren<Collider2D>(true))
                visualCollider.enabled = false;

            foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null) continue;
                string typeName = behaviour.GetType().Name;
                if (typeName == "UnitControl" || typeName == "AnimationEvent")
                    Destroy(behaviour);
            }

            FitAndCenterVisual();
            bodyFacingRoot = visualRoot;
            bodyFacingScale = bodyFacingRoot.localScale;
            headFacingScale = headFacingRoot != null ? headFacingRoot.localScale : Vector3.one;
            if (golemMotion != null) golemMotion.Bind(animator, golemModelRoot);

            CacheClips();
            PlayIdle();
        }

        private void FitAndCenterVisual()
        {
            if (golemModelRoot == null || visualRoot == null) return;
            if (!TryGetActiveVisualBounds(out Bounds bounds)) return;

            if (fitVisualToHeight && bounds.size.y > 0.01f)
            {
                float scale = targetVisualHeight / bounds.size.y;
                golemModelRoot.localScale *= scale;
                if (!TryGetActiveVisualBounds(out bounds)) return;
            }

            Vector3 desiredCenter = visualRoot.position + Vector3.right * visualCenterOffsetX;
            Vector3 correction = new Vector3(
                desiredCenter.x - bounds.center.x,
                visualRoot.position.y + visualGroundOffset - bounds.min.y,
                0f);
            golemModelRoot.position += correction;
        }

        private bool TryGetActiveVisualBounds(out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (SpriteRenderer spriteRenderer in golemModelRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!spriteRenderer.enabled || !spriteRenderer.gameObject.activeInHierarchy ||
                    spriteRenderer.sprite == null) continue;
                if (!found)
                {
                    bounds = spriteRenderer.bounds;
                    found = true;
                }
                else bounds.Encapsulate(spriteRenderer.bounds);
            }
            return found;
        }

        private void CacheClips()
        {
            clipCache.Clear();
            if (animator == null || animator.runtimeAnimatorController == null) return;
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip != null && !clipCache.ContainsKey(clip.name))
                    clipCache.Add(clip.name, clip);
            }
        }

        private static Transform FindNamedTransform(Transform root, string targetName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == targetName) return child;
            return null;
        }

        private void ConfigureHitbox()
        {
            CapsuleCollider2D hitbox = GetComponent<CapsuleCollider2D>();
            if (hitbox == null) hitbox = gameObject.AddComponent<CapsuleCollider2D>();
            hitbox.enabled = true;
            hitbox.isTrigger = true;
            hitbox.direction = CapsuleDirection2D.Vertical;
            hitbox.size = hitboxSize * golemScale;
            hitbox.offset = hitboxOffset * golemScale;
        }

        private void SnapToGround()
        {
            Vector3 ground = GetGroundPoint(transform.position.x);
            transform.position = new Vector3(ground.x, ground.y, transform.position.z);
        }

        public void SetActing(bool acting)
        {
            actingCount = Mathf.Max(0, actingCount + (acting ? 1 : -1));
        }

        public void PlayAnimation(string stateName, float fadeDuration = -1f)
        {
            if (animator == null || string.IsNullOrWhiteSpace(stateName)) return;
            int hash = Animator.StringToHash(stateName);
            if (!animator.HasState(0, hash)) return;
            float fade = fadeDuration >= 0f ? fadeDuration : animationFade;
            currentState = stateName;
            bool sameState = animator.GetCurrentAnimatorStateInfo(0).shortNameHash == hash && !animator.IsInTransition(0);
            if (fade <= 0f || sameState) animator.Play(hash, 0, 0f);
            else animator.CrossFadeInFixedTime(hash, fade, 0, 0f);
        }

        public void SetAnimationSpeed(float speed)
        {
            if (animator != null) animator.speed = Mathf.Max(0.01f, speed);
        }

        public void PlayIdle()
        {
            SetAnimationSpeed(IsPhaseTwo ? phaseTwoAnimationSpeed : 1f);
            if (currentState == idleState && animator != null &&
                animator.GetCurrentAnimatorStateInfo(0).IsName(idleState)) return;
            PlayAnimation(idleState);
        }

        public void PlayLocomotion(bool run)
        {
            SetAnimationSpeed(ActionSpeed);
            string state = run ? RunState : WalkState;
            if (currentState == state) return;
            PlayAnimation(state);
        }

        public float PlayAction(string stateName, float speed)
        {
            SetAnimationSpeed(speed);
            PlayAnimation(stateName, 0.04f);
            return GetClipLength(stateName) / Mathf.Max(0.01f, speed);
        }

        public float GetClipLength(string stateName)
        {
            return clipCache.TryGetValue(stateName, out AnimationClip clip) && clip != null ? clip.length : 1f;
        }

        public float[] GetImpactTimes(string stateName, float speed)
        {
            float safeSpeed = Mathf.Max(0.01f, speed);
            if (!clipCache.TryGetValue(stateName, out AnimationClip clip) || clip == null)
                return new[] { 0.5f / safeSpeed };

            List<float> times = new List<float>();
            foreach (UnityEngine.AnimationEvent evt in clip.events)
            {
                if (evt.functionName != "AttackStart") continue;
                if (times.Count > 0 && Mathf.Abs(times[times.Count - 1] - evt.time) < 0.01f) continue;
                times.Add(evt.time / safeSpeed);
            }
            if (times.Count == 0) times.Add(clip.length * 0.6f / safeSpeed);
            return times.ToArray();
        }

        public Tween MoveTo(float x, float speed, bool run)
        {
            transform.DOKill();
            float distance = Mathf.Abs(x - transform.position.x);
            if (distance < 0.05f) return null;
            SetFacing(x - transform.position.x);
            PlayLocomotion(run);
            float duration = distance / Mathf.Max(0.1f, speed);
            return transform.DOMoveX(x, duration).SetEase(Ease.Linear).SetTarget(transform);
        }

        public Tween PoseVisual(
            Vector2 localOffset,
            float zRotation,
            Vector2 scale,
            float duration,
            Ease? ease = null)
        {
            if (visualRoot == null) return null;
            visualRoot.DOKill();
            float safeDuration = Mathf.Max(0.01f, duration);
            Sequence sequence = DOTween.Sequence().SetTarget(visualRoot);
            sequence.Join(visualRoot.DOLocalMove(visualLocalPosition + (Vector3)localOffset, safeDuration));
            sequence.Join(visualRoot.DOLocalRotate(new Vector3(0f, 0f, zRotation), safeDuration, RotateMode.Fast));
            sequence.Join(visualRoot.DOScale(
                new Vector3(Mathf.Max(0.05f, scale.x), Mathf.Max(0.05f, scale.y), 1f),
                safeDuration
            ));
            sequence.SetEase(ease ?? motionEase);
            return sequence;
        }

        public Tween ResetVisual(float duration = -1f)
        {
            if (visualRoot == null) return null;
            visualRoot.DOKill();
            float safeDuration = duration >= 0f ? duration : motionResetDuration;
            Sequence sequence = DOTween.Sequence().SetTarget(visualRoot);
            sequence.Join(visualRoot.DOLocalMove(visualLocalPosition, safeDuration));
            sequence.Join(visualRoot.DOLocalRotate(Vector3.zero, safeDuration));
            Vector3 resetScale = Vector3.one;
            if (bodyFacingRoot == visualRoot)
                resetScale.x = Mathf.Sign(Mathf.Approximately(visualRoot.localScale.x, 0f)
                    ? 1f
                    : visualRoot.localScale.x);
            sequence.Join(visualRoot.DOScale(resetScale, safeDuration));
            sequence.SetEase(Ease.OutQuad);
            return sequence;
        }

        public Tween ImpactVisual(Vector2 recoilDirection, float strength, float duration = 0.18f)
        {
            if (visualRoot == null) return null;
            visualRoot.DOKill();
            visualRoot.localPosition = visualLocalPosition;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one;
            Vector3 recoil = (Vector3)(recoilDirection.normalized * strength);
            float half = Mathf.Max(0.02f, duration * 0.5f);
            Sequence sequence = DOTween.Sequence().SetTarget(visualRoot);
            sequence.Append(visualRoot.DOLocalMove(visualLocalPosition + recoil, half).SetEase(Ease.OutExpo));
            sequence.Join(visualRoot.DOScale(new Vector3(1.05f, 0.95f, 1f), half));
            sequence.Append(visualRoot.DOLocalMove(visualLocalPosition, half).SetEase(Ease.OutQuad));
            sequence.Join(visualRoot.DOScale(Vector3.one, half));
            return sequence;
        }

        public void FaceTargetImmediately()
        {
            if (Target == null) return;
            SetFacing(Target.position.x - transform.position.x);
        }

        public float FacingToTarget()
        {
            if (Target == null) return 1f;
            return Target.position.x >= transform.position.x ? 1f : -1f;
        }

        public void AnimatePunch(Vector3 readyPosition, Vector3 punchPosition, float direction,
            float readyDuration, float strikeDuration) =>
            golemMotion?.PlayPunch(readyPosition, punchPosition, direction, readyDuration, strikeDuration);

        public void PrepareSawSlam(Vector3 readyPosition, float readyDuration) =>
            golemMotion?.PrepareSawSlam(readyPosition, readyDuration);

        public void StrikeSaw(Vector3 hitPosition, float strikeDuration) =>
            golemMotion?.StrikeSaw(hitPosition, strikeDuration);

        public void AnimateSideSlash(Vector3 startPosition, Vector3 endPosition, float direction,
            float readyDuration, float strikeDuration) =>
            golemMotion?.PlaySideSlash(startPosition, endPosition, direction, readyDuration, strikeDuration);

        public void AnimateFistSlam(Vector3 readyPosition, Vector3 hitPosition,
            float readyDuration, float strikeDuration) =>
            golemMotion?.PlayFistSlam(readyPosition, hitPosition, readyDuration, strikeDuration);

        public void AnimateLaser(Vector3 aimPosition, float readyDuration, float fireDuration) =>
            golemMotion?.PlayLaser(aimPosition, readyDuration, fireDuration);

        public void AnimateMissileCast(float readyDuration, float fireDuration) =>
            golemMotion?.PlayMissileCast(readyDuration, fireDuration);

        public Vector3 GetSawVisualPosition(Vector3 groundPoint) =>
            golemMotion != null ? golemMotion.GetSawImpactPosition(groundPoint) : groundPoint;

        public Vector3 GetFistVisualPosition(Vector3 groundPoint) =>
            golemMotion != null ? golemMotion.GetFistImpactPosition(groundPoint) : groundPoint;

        public void ResetGolemPieces(float duration = 0.25f) =>
            golemMotion?.ResetPieces(duration);

        public Vector3 HeadPosition => golemMotion != null
            ? golemMotion.HeadCenter
            : headFacingRoot != null
                ? headFacingRoot.position
            : transform.position + Vector3.up * (4.2f * golemScale);

        public void SpawnFistImpactRocks(Vector3 point) => SpawnFistRocks(point);
        public void SpawnSawImpactRocks(Vector3 point) => SpawnSawRocks(point);

        protected override void ConfigureRock(
            GameObject rockObject,
            Rigidbody2D rockRigidbody,
            Vector2 launchVelocity,
            float angularVelocity,
            float damage,
            float lifeTime)
        {
            Legacy.VolcanusRock rock = rockObject.GetComponent<Legacy.VolcanusRock>();
            if (rock == null)
            {
                Debug.LogError("Volcanus rock prefab is missing VolcanusRock.", rockObject);
                Destroy(rockObject);
                return;
            }

            rock.Setting(launchVelocity, angularVelocity, damage, lifeTime);
            rock.OnBreak += RockBreak;
        }

        public GolemBoulder SpawnBoulder(Vector3 position, Vector2 velocity)
        {
            if (boulderPrefab == null) return null;
            GolemBoulder boulder = Instantiate(boulderPrefab, position, Quaternion.identity);
            float minScale = Mathf.Min(boulderScaleRange.x, boulderScaleRange.y);
            float maxScale = Mathf.Max(boulderScaleRange.x, boulderScaleRange.y);
            boulder.transform.localScale *= Random.Range(minScale, maxScale);
            boulder.Initialize(
                this,
                velocity,
                boulderBossDamage,
                boulderPlayerDamage,
                playerLayer,
                hazardGroundLayer
            );
            return boulder;
        }

        public GolemGroundWave SpawnGroundWave(
            Vector3 position,
            Vector2 direction,
            float speed,
            float damage,
            float lifeTime)
        {
            if (groundWavePrefab == null) return null;
            GolemGroundWave wave = Instantiate(groundWavePrefab, position, Quaternion.identity);
            wave.transform.localScale *= golemScale;
            wave.Initialize(direction, speed, damage, lifeTime, playerLayer, golemScale);
            return wave;
        }

        public LineRenderer SpawnTelegraphLine(Transform parent = null)
        {
            if (telegraphLinePrefab == null) return null;
            return Instantiate(telegraphLinePrefab, parent);
        }

        public void Shake(bool strong)
        {
            ShakeCamera(strong ? 2.4f : 1.1f);
        }

        public void PlayFeedback(VolcanusFeedbackType type, Vector3 position)
        {
            feedback?.Play(type, position);
        }

        public void TakeDamage(DamageData damage)
        {
            if (IsDead) return;
            feedback?.Play(VolcanusFeedbackType.Damaged, transform.position);
            ApplyBossDamage(damage);
            if (IsDead || IsActing) return;
            SetAnimationSpeed(1f);
            PlayAnimation(hitState, 0.03f);
            currentState = idleState;
        }

        protected override void OnPhaseTwoEntered()
        {
            actingCount = 0;
            transform.DOKill();
            ResetGolemPieces(0.08f);
            ResetVisual(0.08f);
            SetAnimationSpeed(1f);
            PlayAnimation(phaseTwoState, 0.05f);
            currentState = idleState;
            feedback?.Play(VolcanusFeedbackType.PhaseTwo, transform.position);
            Shake(true);
        }

        protected override void OnBossDeath()
        {
            actingCount = 0;
            transform.DOKill();
            ResetGolemPieces(0.05f);
            visualRoot?.DOKill();
            ResetVisual(0.05f);
            SetAnimationSpeed(1f);
            PlayAnimation(deathState, 0.05f);
            feedback?.Play(VolcanusFeedbackType.Death, transform.position);
            ShakeCamera(1.5f);
        }

        protected override void OnDestroy()
        {
            transform.DOKill();
            visualRoot?.DOKill();
            base.OnDestroy();
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = new Color(1f, 0.25f, 0.1f, 0.8f);
            Gizmos.DrawWireCube(
                transform.position + (Vector3)(hitboxOffset * golemScale),
                hitboxSize * golemScale
            );
        }
    }
}
