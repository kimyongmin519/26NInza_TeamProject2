using System.Collections;
using DG.Tweening;
using Member.ODK._01_Script;
using Member.ODK.Scripts.Enemys.Bosses;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    public class LostSoul : PhasedBossController, IDamageable
    {
        [Header("Visual")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer outlineRenderer;
        [SerializeField] private SpriteRenderer darknessOverlay;
        [SerializeField] private Animator animator;
        [SerializeField] private RuntimeAnimatorController flyingController;
        [SerializeField] private RuntimeAnimatorController attackController;
        [SerializeField] private RuntimeAnimatorController teleportAttackController;
        [SerializeField] private RuntimeAnimatorController teleportController;
        [SerializeField] private RuntimeAnimatorController readyController;
        [SerializeField] private RuntimeAnimatorController castController;
        [SerializeField] private RuntimeAnimatorController frenzyController;
        [SerializeField] private RuntimeAnimatorController hitController;
        [SerializeField] private RuntimeAnimatorController deathController;
        [SerializeField] private LostSoulFeedback feedback;
        [SerializeField, Range(0.5f, 1.2f)] private float animationSpeed = 0.82f;
        [SerializeField] private bool prefabFacesRight = true;
        [SerializeField] private int bodySortingOrder = 45;

        [Header("Collision")]
        [SerializeField] private LayerMask playerLayer = 1 << 6;
        [SerializeField] private Vector2 bodyHitboxSize = new Vector2(1.45f, 2.8f);
        [SerializeField] private Vector2 bodyHitboxOffset = new Vector2(0f, 1.35f);

        [Header("Soul Projectile")]
        [SerializeField] private LostSoulProjectile soulProjectilePrefab;
        [SerializeField] private LostSoulGrabbableProjectile weakSoulProjectilePrefab;
        [SerializeField] private LostSoulSlashBeam slashBeamPrefab;
        [SerializeField] private float projectileDamage = 26f;
        [SerializeField] private float thrownSoulDamage = 1f;

        [Header("Phase Two")]
        [SerializeField, Range(0f, 1f)] private float phaseTwoHealthRatio = 0.25f;

        [Header("Animation State")]
        [SerializeField] private string idleState = "ready";
        [SerializeField] private string hitState = "hit";
        [SerializeField] private string deathState = "dead";

        [Header("Swing Timing")]
        [SerializeField, Range(0f, 1f)] private float attackImpactNormalized = 0.5f;
        [SerializeField, Range(0f, 1f)] private float teleportAttackImpactNormalized = 0.5f;
        [SerializeField, Range(0f, 1f)] private float hitAnimationChance = 1f;

        public LayerMask PlayerLayer => playerLayer;
        public float HealthRatio => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;
        public bool IsActing { get; private set; }
        protected override bool HasPhaseTwo => true;

        private Vector3 visualScale;
        private int lastAttackIndex = -1;
        private bool phaseTwoRequested;
        private string loopState;
        private Coroutine oneShotRoutine;
        private Coroutine swingRoutine;

        protected override void Awake()
        {
            base.Awake();
            visualScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
            ConfigureBody();
            SnapToGround();
            if (bodyRenderer != null) bodyRenderer.sortingOrder = bodySortingOrder;
            if (outlineRenderer != null)
            {
                outlineRenderer.sortingOrder = bodySortingOrder + 2;
                outlineRenderer.enabled = false;
            }
            SetDarknessImmediate(0f);
            if (animator != null) animator.speed = animationSpeed;
            PlayAnimation(idleState, 0f);
        }

        protected override IEnumerator PhaseOneLoop()
        {
            if (phaseTwoRequested)
            {
                EnterPhaseTwo();
                yield break;
            }

            int index;
            do index = Random.Range(0, 6);
            while (index == lastAttackIndex);
            lastAttackIndex = index;
            yield return PlayAttack(index);
            yield return AttackWait();
        }

        protected override IEnumerator PhaseTwoLoop()
        {
            yield return PlayAttack(6);
        }

        protected override void Update()
        {
            base.Update();
            if (!IsDead && !IsPhaseTwo && HealthRatio <= phaseTwoHealthRatio)
                phaseTwoRequested = true;
        }

        private void LateUpdate()
        {
            if (Target != null && visualRoot != null)
            {
                bool targetRight = Target.position.x >= transform.position.x;
                float direction = targetRight == prefabFacesRight ? 1f : -1f;
                Vector3 scale = visualScale;
                scale.x = Mathf.Abs(visualScale.x) * direction;
                visualRoot.localScale = scale;
            }

            if (outlineRenderer != null && bodyRenderer != null)
            {
                outlineRenderer.sprite = bodyRenderer.sprite;
                outlineRenderer.flipX = bodyRenderer.flipX;
                outlineRenderer.flipY = bodyRenderer.flipY;
            }
        }

        private void ConfigureBody()
        {
            CapsuleCollider2D hitbox = GetComponent<CapsuleCollider2D>();
            if (hitbox == null) hitbox = gameObject.AddComponent<CapsuleCollider2D>();
            hitbox.direction = CapsuleDirection2D.Vertical;
            hitbox.size = bodyHitboxSize;
            hitbox.offset = bodyHitboxOffset;
        }

        private void SnapToGround()
        {
            Vector3 point = GetGroundPoint(transform.position.x) + Vector3.up * 0.05f;
            point.z = transform.position.z;
            transform.position = point;
        }

        public void PlayAnimation(string stateName, float fade = 0.04f, float normalizedTime = 0f)
        {
            if (string.IsNullOrWhiteSpace(stateName)) return;
            if (IsOneShotState(stateName))
            {
                PlayOneShot(stateName, normalizedTime);
                return;
            }

            CancelSwing();
            CancelOneShot();
            loopState = stateName;
            PlayRaw(stateName, fade, normalizedTime);
        }

        public void PlayIdle(bool force = true)
        {
            loopState = idleState;
            if (!force && oneShotRoutine != null) return;
            CancelSwing();
            CancelOneShot();
            PlayRaw(idleState, 0.04f, 0f);
        }

        public void SetActing(bool acting)
        {
            IsActing = acting;
        }

        public void PlaySwing(string stateName = "attack", float timeUntilImpact = 0f, System.Action onImpact = null)
        {
            CancelSwing();
            swingRoutine = StartCoroutine(SwingRoutine(stateName, Mathf.Max(0f, timeUntilImpact), onImpact));
        }

        public float GetImpactDelay(string stateName = "attack")
        {
            return GetStateDuration(stateName) * GetImpactNormalized(stateName);
        }

        private IEnumerator SwingRoutine(string stateName, float timeUntilImpact, System.Action onImpact)
        {
            float duration = GetStateDuration(stateName);
            float impactNormalized = GetImpactNormalized(stateName);
            float impactOffset = duration * impactNormalized;

            if (timeUntilImpact >= impactOffset)
            {
                float lead = timeUntilImpact - impactOffset;
                if (lead > 0f) yield return new WaitForSeconds(lead);
                PlayOneShot(stateName, 0f);
                if (impactOffset > 0f) yield return new WaitForSeconds(impactOffset);
            }
            else
            {
                float startNormalized = duration > 0f
                    ? Mathf.Clamp01((impactOffset - timeUntilImpact) / duration)
                    : impactNormalized;
                PlayOneShot(stateName, startNormalized);
                if (timeUntilImpact > 0f) yield return new WaitForSeconds(timeUntilImpact);
            }

            swingRoutine = null;
            onImpact?.Invoke();
        }

        private void PlayOneShot(string stateName, float normalizedTime)
        {
            CancelOneShot();
            if (string.IsNullOrEmpty(loopState)) loopState = idleState;
            PlayRaw(stateName, 0f, normalizedTime);
            float remaining = GetStateDuration(stateName) * (1f - Mathf.Clamp01(normalizedTime));
            oneShotRoutine = StartCoroutine(ReturnToLoop(remaining));
        }

        private IEnumerator ReturnToLoop(float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            oneShotRoutine = null;
            if (IsDead) yield break;
            PlayRaw(string.IsNullOrEmpty(loopState) ? idleState : loopState, 0.03f, 0f);
        }

        private void CancelOneShot()
        {
            if (oneShotRoutine == null) return;
            StopCoroutine(oneShotRoutine);
            oneShotRoutine = null;
        }

        private void CancelSwing()
        {
            if (swingRoutine == null) return;
            StopCoroutine(swingRoutine);
            swingRoutine = null;
        }

        private static bool IsOneShotState(string stateName)
        {
            return stateName == "attack" || stateName == "teleport attack" || stateName == "hit";
        }

        private float GetImpactNormalized(string stateName)
        {
            return stateName == "teleport attack" ? teleportAttackImpactNormalized : attackImpactNormalized;
        }

        private RuntimeAnimatorController GetController(string stateName)
        {
            return stateName switch
            {
                "flying" => flyingController,
                "attack" => attackController,
                "teleport attack" => teleportAttackController,
                "teleport" => teleportController,
                "ready" => readyController,
                "cast" => castController,
                "frenzy" => frenzyController,
                "hit" => hitController,
                "dead" => deathController,
                _ => null
            };
        }

        private float GetStateDuration(string stateName)
        {
            RuntimeAnimatorController controller = GetController(stateName);
            if (controller == null) controller = animator != null ? animator.runtimeAnimatorController : null;
            if (controller == null || controller.animationClips == null || controller.animationClips.Length == 0)
                return 0.5f;
            float length = controller.animationClips[0].length;
            float speed = animator != null ? Mathf.Max(0.01f, animator.speed) : 1f;
            return length / speed;
        }

        private void PlayRaw(string stateName, float fade, float normalizedTime)
        {
            if (animator == null || string.IsNullOrWhiteSpace(stateName)) return;
            RuntimeAnimatorController controller = GetController(stateName);
            bool controllerChanged = controller != null && animator.runtimeAnimatorController != controller;
            if (controllerChanged)
                animator.runtimeAnimatorController = controller;

            int hash = Animator.StringToHash(stateName);
            if (!animator.HasState(0, hash)) return;

            float startTime = Mathf.Clamp01(normalizedTime);
            if (controllerChanged || fade <= 0f)
            {
                animator.Play(hash, 0, startTime);
                animator.Update(0f);
                return;
            }

            animator.CrossFade(hash, fade, 0, startTime);
        }

        public Vector3 TeleportToTarget(float sideDistance = 0f)
        {
            if (Target == null) return transform.position;
            float side = Random.value < 0.5f ? -1f : 1f;
            float x = Target.position.x + side * sideDistance;
            x = Mathf.Clamp(x, ArenaCenter.x - ArenaHalfWidth + 1f, ArenaCenter.x + ArenaHalfWidth - 1f);
            Vector3 point = GetGroundPoint(x) + Vector3.up * 0.05f;
            point.z = transform.position.z;
            transform.DOKill();
            transform.position = point;
            feedback?.PlayTeleport();
            return point;
        }

        public Vector3 MoveToArenaCenter()
        {
            Vector3 point = GetGroundPoint(ArenaCenter.x) + Vector3.up * 0.05f;
            point.z = transform.position.z;
            transform.position = point;
            feedback?.PlayCenterMove();
            return point;
        }

        public LostSoulProjectile SpawnSoul(Vector3 position, Vector2 velocity, float damageScale = 1f)
        {
            if (soulProjectilePrefab == null) return null;
            LostSoulProjectile soul = ODKPool.Spawn(soulProjectilePrefab, position, Quaternion.identity);
            if (soul == null) return null;
            soul.Initialize(velocity, projectileDamage * damageScale, playerLayer, Target);
            feedback?.PlaySoulProjectile();
            return soul;
        }

        public LostSoulGrabbableProjectile SpawnWeakSoul(Vector3 position, Vector2 velocity)
        {
            if (weakSoulProjectilePrefab == null) return null;
            LostSoulGrabbableProjectile soul = ODKPool.Spawn(weakSoulProjectilePrefab, position, Quaternion.identity);
            if (soul == null) return null;
            soul.Initialize(this, velocity, thrownSoulDamage, Target);
            feedback?.PlayWeakSoul();
            return soul;
        }

        public LostSoulSlashBeam SpawnSlashBeam(
            Vector3 origin,
            Vector2 direction,
            float length,
            float width,
            float warningDuration,
            float activeDuration,
            float damage,
            Color color,
            float preFireRotationDegrees = 0f)
        {
            if (slashBeamPrefab == null) return null;
            LostSoulSlashBeam beam = ODKPool.Spawn(slashBeamPrefab, origin, Quaternion.identity);
            if (beam == null) return null;
            beam.Initialize(this, origin, direction, length, width, warningDuration, activeDuration, damage, color, preFireRotationDegrees);
            return beam;
        }

        public void SetDarkness(bool enabled, float duration = 0.18f, float darknessAlpha = 0.88f)
        {
            if (darknessOverlay == null) return;
            if (enabled) feedback?.PlayFadeOut();
            darknessOverlay.DOKill();
            Color target = darknessOverlay.color;
            target.a = enabled ? Mathf.Clamp01(darknessAlpha) : 0f;
            darknessOverlay.DOColor(target, duration).SetEase(Ease.OutQuad);
        }

        public IEnumerator FlashDarknessReveal(float duration)
        {
            if (darknessOverlay == null) yield break;
            darknessOverlay.DOKill();
            Color reveal = new Color(0.8f, 0.9f, 1f, 0.035f);
            darknessOverlay.color = reveal;
            Color darkness = new Color(0.015f, 0.008f, 0.03f, 1f);
            Sequence sequence = DOTween.Sequence().SetTarget(darknessOverlay);
            sequence.AppendInterval(Mathf.Min(0.025f, duration * 0.16f));
            sequence.Append(darknessOverlay.DOColor(new Color(1f, 0.94f, 1f, 0.32f),
                Mathf.Min(0.025f, duration * 0.16f)).SetEase(Ease.OutFlash));
            sequence.Append(darknessOverlay.DOColor(darkness, Mathf.Max(0.02f, duration - 0.05f))
                .SetEase(Ease.InQuad));
            yield return sequence.WaitForCompletion();
        }

        private void SetDarknessImmediate(float alpha)
        {
            if (darknessOverlay == null) return;
            Color color = darknessOverlay.color;
            color.a = alpha;
            darknessOverlay.color = color;
        }

        public IEnumerator PulseOutline(float duration)
        {
            if (outlineRenderer == null) yield break;
            outlineRenderer.enabled = true;
            Color color = outlineRenderer.color;
            color.a = 1f;
            outlineRenderer.color = color;
            yield return new WaitForSeconds(duration);
            outlineRenderer.enabled = false;
        }

        public static void SetLine(LineRenderer line, Vector3 start, Vector3 end, Color color, float width)
        {
            if (line == null) return;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startColor = color;
            line.endColor = color;
            line.widthMultiplier = width;
            line.enabled = true;
        }

        public void TakeDamage(DamageData damage)
        {
            if (IsDead) return;
            ApplyBossDamage(damage);
            if (!IsDead)
            {
                feedback?.PlayHit();
                if (!IsActing && oneShotRoutine == null && swingRoutine == null && Random.value <= hitAnimationChance)
                    PlayOneShot(hitState, 0f);
            }
        }

        protected override void OnPhaseTwoEntered()
        {
            phaseTwoRequested = false;
            IsActing = false;
            swingRoutine = null;
            oneShotRoutine = null;
            PlayIdle();
            ShakeCamera(0.9f);
        }

        [ContextMenu("Enter Lost Soul Phase Two (25%)")]
        private void EnterPhaseTwoFromContext() => EnterPhaseTwoAtHealth(0.25f);

        [ContextMenu("Enter Lost Soul Rotating Slashes (20%)")]
        private void EnterRotatingSlashesFromContext() => EnterPhaseTwoAtHealth(0.2f);

        [ContextMenu("Enter Lost Soul Enrage (10%)")]
        private void EnterEnrageFromContext() => EnterPhaseTwoAtHealth(0.099f);

        private void EnterPhaseTwoAtHealth(float ratio)
        {
            float targetHealth = MaxHealth * Mathf.Clamp01(ratio);
            if (HealthModule != null && MaxHealth > 0f && CurrentHealth > targetHealth)
            {
                float amount = CurrentHealth - targetHealth;
                HealthModule.ApplyDamage(new DamageData(amount, DamageType.Special));
            }
            phaseTwoRequested = true;
        }

        protected override void OnBossDeath()
        {
            IsActing = false;
            swingRoutine = null;
            oneShotRoutine = null;
            transform.DOKill();
            SetDarkness(false, 0.08f);
            if (outlineRenderer != null) outlineRenderer.enabled = false;
            feedback?.PlayDeath();
            ShakeCamera(1.2f);
            PlayAnimation(deathState, 0.03f);
        }

        public void PlaySlashFeedback() => feedback?.PlaySlash();
        public void PlayFadeSlashFeedback() => feedback?.PlayFadeSlash();
        public void PlayDesperationScreamFeedback() => feedback?.PlayDesperationScream();
        public void PlayCastFeedback() => feedback?.PlayCast();
        public void PlayBeamWarningFeedback() => feedback?.PlayBeamWarning();
        public void PlayBeamFireFeedback() => feedback?.PlayBeamFire();
        public void PlayDirectionCueFeedback() => feedback?.PlayDirectionCue();
    }
}
