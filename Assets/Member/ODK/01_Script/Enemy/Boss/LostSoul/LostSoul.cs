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
        [SerializeField] private bool prefabFacesRight = true;
        [SerializeField] private int bodySortingOrder = 45;

        [Header("Collision")]
        [SerializeField] private LayerMask playerLayer = 1 << 6;
        [SerializeField] private LayerMask groundLayer = 1 << 3;
        [SerializeField] private Vector2 bodyHitboxSize = new Vector2(1.45f, 2.8f);
        [SerializeField] private Vector2 bodyHitboxOffset = new Vector2(0f, 1.35f);

        [Header("Soul Projectile")]
        [SerializeField] private LostSoulProjectile soulProjectilePrefab;
        [SerializeField] private LostSoulGrabbableProjectile weakSoulProjectilePrefab;
        [SerializeField] private float projectileDamage = 26f;
        [SerializeField] private float thrownSoulDamage = 85f;

        [Header("Animation State")]
        [SerializeField] private string idleState = "flying";
        [SerializeField] private string hitState = "hit";
        [SerializeField] private string deathState = "dead";

        public LayerMask PlayerLayer => playerLayer;
        public LayerMask GroundLayer => groundLayer;
        protected override bool HasPhaseTwo => false;

        private Vector3 visualScale;
        private int lastAttackIndex = -1;

        protected override void Awake()
        {
            base.Awake();
            visualScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
            ConfigureBody();
            if (bodyRenderer != null) bodyRenderer.sortingOrder = bodySortingOrder;
            if (outlineRenderer != null)
            {
                outlineRenderer.sortingOrder = bodySortingOrder + 2;
                outlineRenderer.enabled = false;
            }
            SetDarknessImmediate(0f);
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

        protected override IEnumerator PhaseTwoLoop() => PhaseOneLoop();

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

        public void PlayAnimation(string stateName, float fade = 0.04f)
        {
            if (animator == null || string.IsNullOrWhiteSpace(stateName)) return;
            int hash = Animator.StringToHash(stateName);
            if (animator.HasState(0, hash)) animator.CrossFade(hash, fade, 0, 0f);
        }

        public void PlayIdle() => PlayAnimation(idleState);

        public Vector3 TeleportToTarget(float sideDistance = 0f, float airHeight = 0f)
        {
            if (Target == null) return transform.position;
            float side = Random.value < 0.5f ? -1f : 1f;
            float x = Target.position.x + side * sideDistance;
            x = Mathf.Clamp(x, ArenaCenter.x - ArenaHalfWidth + 1f, ArenaCenter.x + ArenaHalfWidth - 1f);
            Vector3 point = airHeight > 0f
                ? GetGroundPoint(x) + Vector3.up * airHeight
                : new Vector3(x, Target.position.y, transform.position.z);
            point.z = transform.position.z;
            transform.DOKill();
            transform.position = point;
            return point;
        }

        public Vector3 MoveToArenaCenter()
        {
            Vector3 point = GetGroundPoint(ArenaCenter.x) + Vector3.up * 2.8f;
            point.z = transform.position.z;
            transform.position = point;
            return point;
        }

        public LostSoulProjectile SpawnSoul(Vector3 position, Vector2 velocity, float damageScale = 1f)
        {
            if (soulProjectilePrefab == null) return null;
            LostSoulProjectile soul = Instantiate(soulProjectilePrefab, position, Quaternion.identity);
            soul.Initialize(velocity, projectileDamage * damageScale, playerLayer);
            return soul;
        }

        public LostSoulGrabbableProjectile SpawnWeakSoul(Vector3 position, Vector2 velocity)
        {
            if (weakSoulProjectilePrefab == null) return null;
            LostSoulGrabbableProjectile soul = Instantiate(weakSoulProjectilePrefab, position, Quaternion.identity);
            soul.Initialize(this, velocity, projectileDamage * 0.45f, thrownSoulDamage, playerLayer);
            return soul;
        }

        public void SetDarkness(bool enabled, float duration = 0.18f)
        {
            if (darknessOverlay == null) return;
            darknessOverlay.DOKill();
            Color target = darknessOverlay.color;
            target.a = enabled ? 0.88f : 0f;
            darknessOverlay.DOColor(target, duration).SetEase(Ease.OutQuad);
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
            if (!IsDead) PlayAnimation(hitState, 0.02f);
        }

        protected override void OnPhaseTwoEntered() { }

        protected override void OnBossDeath()
        {
            transform.DOKill();
            SetDarkness(false, 0.08f);
            if (outlineRenderer != null) outlineRenderer.enabled = false;
            PlayAnimation(deathState, 0.03f);
        }
    }
}
