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
        [SerializeField] private float thrownSoulDamage = 85f;

        [Header("Animation State")]
        [SerializeField] private string idleState = "ready";
        [SerializeField] private string hitState = "hit";
        [SerializeField] private string deathState = "dead";

        public LayerMask PlayerLayer => playerLayer;
        public float HealthRatio => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;
        protected override bool HasPhaseTwo => true;

        private Vector3 visualScale;
        private int lastAttackIndex = -1;

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
            if (!IsDead && !IsPhaseTwo && HealthRatio <= 0.25f)
                EnterPhaseTwo();
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
            if (animator == null || string.IsNullOrWhiteSpace(stateName)) return;
            RuntimeAnimatorController controller = stateName switch
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

        public void PlayIdle() => PlayAnimation(idleState);

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
            LostSoulProjectile soul = Instantiate(soulProjectilePrefab, position, Quaternion.identity);
            soul.Initialize(velocity, projectileDamage * damageScale, playerLayer, Target);
            feedback?.PlaySoulProjectile();
            return soul;
        }

        public LostSoulGrabbableProjectile SpawnWeakSoul(Vector3 position, Vector2 velocity)
        {
            if (weakSoulProjectilePrefab == null) return null;
            LostSoulGrabbableProjectile soul = Instantiate(weakSoulProjectilePrefab, position, Quaternion.identity);
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
            LostSoulSlashBeam beam = Instantiate(slashBeamPrefab, origin, Quaternion.identity);
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
                PlayAnimation(hitState, 0.02f);
            }
        }

        protected override void OnPhaseTwoEntered() => ShakeCamera(0.9f);

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
            EnterPhaseTwo();
        }

        protected override void OnBossDeath()
        {
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
