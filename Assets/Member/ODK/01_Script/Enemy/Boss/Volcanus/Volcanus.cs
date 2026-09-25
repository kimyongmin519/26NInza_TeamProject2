using System.Collections;
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
        [Header("Golem Visual")]
        [SerializeField] private Transform visualMotionRoot;
        [SerializeField] private Transform golemModelRoot;
        [SerializeField] private Vector3 visualLocalPosition;
        [SerializeField] private Vector3 visualLocalScale = Vector3.one;
        [SerializeField] private bool prefabFacesRight = true;
        [SerializeField] private int visualSortingOrder = 20;

        [Header("Golem Motion")]
        [SerializeField] private float motionResetDuration = 0.16f;
        [SerializeField] private Ease motionEase = Ease.OutCubic;

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
        [SerializeField] private float boulderBossDamage = 90f;
        [SerializeField] private float boulderPlayerDamage = 30f;
        [SerializeField] private Vector2 boulderScaleRange = new Vector2(0.7f, 1.45f);

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string idleState = "idle_1";
        [SerializeField] private string phaseTwoState = "idle_2";
        [SerializeField] private string hitState = "hit_1";
        [SerializeField] private string deathState = "death";
        [SerializeField] private float phaseTransitionDuration = 1f;

        [Header("Feedback")]
        [SerializeField] private VolcanusFeedback feedback;
        [SerializeField] private CinemachineImpulseSource impulseSource;

        public LayerMask PlayerLayer => playerLayer;
        public LayerMask GroundLayer => hazardGroundLayer;
        public Animator Animator => animator;
        protected override float PhaseTransitionDelay => phaseTransitionDuration;

        private Transform visualRoot;
        private Vector3 bodyFacingScale;
        private Vector3 headFacingScale;
        private int lastAttackIndex = -1;

        private const int ComboIndex = 0;
        private const int QuakeIndex = 1;
        private const int LeapSlamIndex = 2;
        private const int BoulderRainIndex = 3;
        private const int AttackCount = 4;

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
            yield return PlayRandomAttack(1f);
            yield return AttackWait();
        }

        protected override IEnumerator PhaseTwoLoop()
        {
            yield return PlayRandomAttack(1.15f);
            yield return AttackWait();
        }

        private IEnumerator PlayRandomAttack(float durationScale)
        {
            int index;
            do index = Random.Range(0, AttackCount);
            while (index == lastAttackIndex);
            lastAttackIndex = index;
            yield return PlayAttack(index, durationScale);
        }

        private void LateUpdate()
        {
            if (!faceTarget || Target == null || bodyFacingRoot == null) return;
            bool targetIsRight = Target.position.x >= transform.position.x;
            float direction = targetIsRight == prefabFacesRight ? 1f : -1f;
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
                if (visualMotionRoot != null && oldRenderer.transform.IsChildOf(visualMotionRoot))
                    continue;
                oldRenderer.enabled = false;
            }
            foreach (Collider2D oldCollider in GetComponentsInChildren<Collider2D>(true))
                oldCollider.enabled = false;
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
                Debug.LogError(
                    "[Volcanus] Scene child Boss Golem Visual is not connected.",
                    this
                );
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
            golemModelRoot.localScale = Vector3.Scale(golemModelRoot.localScale, visualLocalScale);
            bodyFacingRoot = golemModelRoot;
            headFacingRoot = FindNamedTransform(golemModelRoot, "head_1") ??
                FindNamedTransform(golemModelRoot, "head_2") ??
                FindNamedTransform(golemModelRoot, "head_3");
            bodyFacingScale = bodyFacingRoot.localScale;
            headFacingScale = headFacingRoot != null ? headFacingRoot.localScale : Vector3.one;
            animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.enabled = true;
                animator.Rebind();
                animator.Update(0f);
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
                string typeName = behaviour.GetType().Name;
                if (typeName == "UnitControl" || typeName == "AnimationEvent")
                    behaviour.enabled = false;
            }

            PlayIdle();
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
            hitbox.direction = CapsuleDirection2D.Vertical;
            hitbox.size = hitboxSize;
            hitbox.offset = hitboxOffset;
        }

        private void SnapToGround()
        {
            Vector3 ground = GetGroundPoint(transform.position.x);
            transform.position = new Vector3(ground.x, ground.y, transform.position.z);
        }

        public void PlayAnimation(string stateName, float fadeDuration = 0.08f)
        {
            if (animator == null || string.IsNullOrWhiteSpace(stateName)) return;
            int hash = Animator.StringToHash(stateName);
            if (animator.HasState(0, hash)) animator.CrossFade(hash, fadeDuration, 0, 0f);
        }

        public void SetAnimationSpeed(float speed)
        {
            if (animator != null) animator.speed = Mathf.Max(0.01f, speed);
        }

        public void PlayIdle() => PlayAnimation(IsPhaseTwo ? phaseTwoState : idleState);

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
            sequence.Join(visualRoot.DOLocalMove(
                visualLocalPosition + (Vector3)localOffset,
                safeDuration
            ));
            sequence.Join(visualRoot.DOLocalRotate(
                new Vector3(0f, 0f, zRotation),
                safeDuration,
                RotateMode.Fast
            ));
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
            sequence.Join(visualRoot.DOScale(Vector3.one, safeDuration));
            sequence.SetEase(Ease.OutBack);
            return sequence;
        }

        public Tween ImpactVisual(Vector2 recoilDirection, float strength, float duration = 0.18f)
        {
            if (visualRoot == null) return null;
            visualRoot.DOKill();
            Vector3 recoil = (Vector3)(recoilDirection.normalized * strength);
            float half = Mathf.Max(0.02f, duration * 0.5f);
            Sequence sequence = DOTween.Sequence().SetTarget(visualRoot);
            sequence.Append(visualRoot.DOLocalMove(visualLocalPosition + recoil, half)
                .SetEase(Ease.OutExpo));
            sequence.Join(visualRoot.DOScale(new Vector3(1.08f, 0.9f, 1f), half));
            sequence.Append(visualRoot.DOLocalMove(visualLocalPosition, half)
                .SetEase(Ease.OutBack));
            sequence.Join(visualRoot.DOScale(Vector3.one, half));
            return sequence;
        }

        public void FaceTargetImmediately()
        {
            if (Target == null) return;
            LateUpdate();
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
            wave.Initialize(direction, speed, damage, lifeTime, playerLayer);
            return wave;
        }

        public LineRenderer SpawnTelegraphLine(Transform parent = null)
        {
            if (telegraphLinePrefab == null) return null;
            return Instantiate(telegraphLinePrefab, parent);
        }

        public void Shake(bool strong)
        {
            impulseSource?.GenerateImpulse(strong ? 2.4f : 1.1f);
        }

        public void PlayFeedback(VolcanusFeedbackType type, Vector3 position)
        {
            feedback?.Play(type, position);
        }

        public void TakeDamage(DamageData damage)
        {
            if (IsDead) return;
            SetAnimationSpeed(1f);
            PlayAnimation(hitState);
            feedback?.Play(VolcanusFeedbackType.Damaged, transform.position);
            ApplyBossDamage(damage);
        }

        protected override void OnPhaseTwoEntered()
        {
            SetAnimationSpeed(1f);
            PlayAnimation(phaseTwoState);
            PoseVisual(
                new Vector2(0f, 0.45f),
                0f,
                new Vector2(1.14f, 0.88f),
                0.3f,
                Ease.OutExpo
            )?.OnComplete(() => ResetVisual(0.42f));
            feedback?.Play(VolcanusFeedbackType.PhaseTwo, transform.position);
            Shake(true);
        }

        protected override void OnBossDeath()
        {
            visualRoot?.DOKill();
            SetAnimationSpeed(1f);
            PlayAnimation(deathState, 0.05f);
            PoseVisual(
                new Vector2(0f, -0.35f),
                prefabFacesRight ? -10f : 10f,
                new Vector2(1.08f, 0.9f),
                0.5f,
                Ease.InQuad
            );
            feedback?.Play(VolcanusFeedbackType.Death, transform.position);
        }

        protected override void OnDestroy()
        {
            visualRoot?.DOKill();
            base.OnDestroy();
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = new Color(1f, 0.25f, 0.1f, 0.8f);
            Gizmos.DrawWireCube(transform.position + (Vector3)hitboxOffset, hitboxSize);
        }
    }
}
