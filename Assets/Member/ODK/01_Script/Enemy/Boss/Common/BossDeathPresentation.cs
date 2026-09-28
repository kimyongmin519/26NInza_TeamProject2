using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using KimLIb.SoundSystem;
using UnityEngine;
using UnityEngine.Events;

namespace Member.ODK.Scripts.Enemys.Bosses
{
    [DisallowMultipleComponent]
    public sealed class BossDeathPresentation : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer[] renderers;
        [SerializeField] private bool collectChildRenderersOnPlay = true;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float animationLeadTime = 0.55f;
        [SerializeField, Min(0.05f)] private float breakDuration = 0.25f;
        [SerializeField, Min(0.05f)] private float vanishDuration = 0.7f;

        [Header("Motion")]
        [SerializeField] private Vector3 vanishLocalOffset = new Vector3(0f, -1.5f, 0f);
        [SerializeField] private Vector3 vanishScale = new Vector3(0.85f, 0.15f, 1f);
        [SerializeField] private float vanishRotation = -12f;
        [SerializeField, Min(0f)] private float shakeStrength = 0.18f;
        [SerializeField, Min(1)] private int shakeVibrato = 14;
        [SerializeField, Min(0f)] private float fragmentScatter = 0.75f;
        [SerializeField] private float fragmentLift = 0.6f;
        [SerializeField, Min(0f)] private float fragmentRotation = 35f;
        [SerializeField] private Ease vanishEase = Ease.InCubic;

        [Header("Visual")]
        [SerializeField] private Color deathTint = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField, Range(0, 4)] private int flashCount = 2;
        [SerializeField, Min(0.02f)] private float flashDuration = 0.12f;
        [SerializeField] private bool hideRenderersOnComplete = true;
        [SerializeField] private bool disableCollidersOnStart = true;

        [Header("Feedback Slots")]
        [SerializeField] private SoundClipSO deathSound;
        [SerializeField, Min(0f)] private float cameraShake = 1.2f;
        [SerializeField] private UnityEvent onDeathStart;
        [SerializeField] private UnityEvent onDeathBreak;
        [SerializeField] private UnityEvent onDeathComplete;

        private Sequence sequence;
        private bool played;

        public bool IsPlaying => sequence != null && sequence.IsActive() && sequence.IsPlaying();
        public float Duration => animationLeadTime + flashCount * flashDuration + breakDuration + vanishDuration;

        public IEnumerator Play(PhasedBossController owner)
        {
            if (played) yield break;
            played = true;

            if (visualRoot == null) visualRoot = transform;
            if (collectChildRenderersOnPlay || renderers == null || renderers.Length == 0)
                renderers = visualRoot.GetComponentsInChildren<SpriteRenderer>(true);

            DisableGameplayCollision();
            ODKSoundPlayback.Play(deathSound, transform.position);
            onDeathStart?.Invoke();

            Vector3 startPosition = visualRoot.localPosition;
            Vector3 startScale = visualRoot.localScale;
            Quaternion startRotation = visualRoot.localRotation;
            Color[] startColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                startColors[i] = renderers[i] != null ? renderers[i].color : Color.white;
            List<Transform> fragments = CollectVisualBranches();

            sequence?.Kill(false);
            sequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            if (animationLeadTime > 0f) sequence.AppendInterval(animationLeadTime);
            for (int flashIndex = 0; flashIndex < flashCount; flashIndex++)
            {
                Sequence flashIn = DOTween.Sequence();
                Sequence flashOut = DOTween.Sequence();
                for (int i = 0; i < renderers.Length; i++)
                {
                    SpriteRenderer renderer = renderers[i];
                    if (renderer == null || !renderer.gameObject.activeInHierarchy) continue;
                    flashIn.Join(renderer.DOColor(flashColor, flashDuration * 0.45f));
                }
                for (int i = 0; i < renderers.Length; i++)
                {
                    SpriteRenderer renderer = renderers[i];
                    if (renderer == null || !renderer.gameObject.activeInHierarchy) continue;
                    flashOut.Join(renderer.DOColor(startColors[i], flashDuration * 0.55f));
                }
                Sequence flash = DOTween.Sequence().Append(flashIn).Append(flashOut);
                sequence.Append(flash);
            }
            sequence.AppendCallback(() =>
            {
                owner?.ShakeCamera(cameraShake);
                onDeathBreak?.Invoke();
            });
            sequence.Append(visualRoot.DOShakePosition(
                breakDuration,
                shakeStrength,
                shakeVibrato,
                70f,
                false,
                true,
                ShakeRandomnessMode.Harmonic));
            sequence.Join(visualRoot.DOShakeRotation(
                breakDuration,
                new Vector3(0f, 0f, shakeStrength * 55f),
                shakeVibrato,
                65f,
                true,
                ShakeRandomnessMode.Harmonic));
            sequence.Append(visualRoot.DOLocalMove(startPosition + vanishLocalOffset, vanishDuration).SetEase(vanishEase));
            sequence.Join(visualRoot.DOScale(Vector3.Scale(startScale, vanishScale), vanishDuration).SetEase(vanishEase));
            sequence.Join(visualRoot.DOLocalRotateQuaternion(
                startRotation * Quaternion.Euler(0f, 0f, vanishRotation),
                vanishDuration).SetEase(vanishEase));

            for (int i = 0; i < fragments.Count; i++)
            {
                Transform fragment = fragments[i];
                if (fragment == null) continue;
                float side = fragments.Count <= 1
                    ? 0f
                    : Mathf.Lerp(-1f, 1f, i / (float)(fragments.Count - 1));
                float wave = Mathf.Sin((i + 1f) * 2.17f);
                Vector3 offset = new Vector3(
                    side * fragmentScatter,
                    fragmentLift + Mathf.Abs(wave) * fragmentScatter * 0.35f,
                    0f);
                sequence.Join(fragment.DOLocalMove(fragment.localPosition + offset, vanishDuration).SetEase(Ease.OutQuad));
                sequence.Join(fragment.DOLocalRotate(
                    fragment.localEulerAngles + new Vector3(0f, 0f, side * fragmentRotation),
                    vanishDuration,
                    RotateMode.FastBeyond360).SetEase(Ease.OutQuad));
            }

            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer == null) continue;
                Color targetColor = Color.Lerp(renderer.color, deathTint, 0.65f);
                targetColor.a = 0f;
                sequence.Join(renderer.DOColor(targetColor, vanishDuration).SetEase(Ease.InQuad));
            }

            sequence.OnComplete(() =>
            {
                if (hideRenderersOnComplete)
                {
                    foreach (SpriteRenderer renderer in renderers)
                        if (renderer != null) renderer.enabled = false;
                }
                onDeathComplete?.Invoke();
            });

            yield return sequence.WaitForCompletion();
        }

        public void Cancel()
        {
            sequence?.Kill(false);
            sequence = null;
        }

        private void DisableGameplayCollision()
        {
            if (disableCollidersOnStart)
            {
                Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
                foreach (Collider2D collider in colliders)
                    if (collider != null) collider.enabled = false;
            }

            Rigidbody2D[] bodies = GetComponentsInChildren<Rigidbody2D>(true);
            foreach (Rigidbody2D body in bodies)
            {
                if (body == null) continue;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.simulated = false;
            }
        }

        private List<Transform> CollectVisualBranches()
        {
            List<Transform> branches = new List<Transform>();
            HashSet<Transform> unique = new HashSet<Transform>();
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer == null || !renderer.gameObject.activeInHierarchy) continue;
                Transform branch = renderer.transform;
                while (branch.parent != null && branch.parent != visualRoot)
                    branch = branch.parent;
                if (branch == visualRoot || !unique.Add(branch)) continue;
                branches.Add(branch);
            }
            return branches;
        }

        private void OnDisable() => Cancel();
        private void OnDestroy() => Cancel();
    }
}
