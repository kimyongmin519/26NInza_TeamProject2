using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using KimLIb.SoundSystem;
using UnityEngine;
using UnityEngine.Events;

namespace Member.ODK.Scripts.Enemys.Bosses
{
    [DisallowMultipleComponent]
    public sealed class BossSummonPresentation : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer[] renderers;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float delay = 0.35f;
        [SerializeField, Min(0.05f)] private float appearDuration = 0.65f;
        [SerializeField, Min(0f)] private float settleDuration = 0.3f;

        [Header("Motion")]
        [SerializeField] private Vector3 startLocalOffset = new Vector3(0f, 4f, 0f);
        [SerializeField] private float startRotation = 18f;
        [SerializeField] private Vector3 impactPunch = new Vector3(0f, 0.18f, 0f);
        [SerializeField, Min(0.02f)] private float impactDuration = 0.1f;
        [SerializeField] private Ease appearEase = Ease.OutCubic;

        [Header("Flash")]
        [SerializeField] private Color summonTint = Color.white;
        [SerializeField, Range(0f, 1f)] private float startAlpha;
        [SerializeField, Min(0f)] private float flashDuration = 0.12f;

        [Header("Feedback Slots")]
        [SerializeField] private SoundClipSO summonSound;
        [SerializeField, Min(0f)] private float cameraShake = 0.65f;
        [SerializeField] private UnityEvent onSummonStart;
        [SerializeField] private UnityEvent onSummonImpact;
        [SerializeField] private UnityEvent onSummonComplete;

        private readonly List<Color> originalColors = new List<Color>();
        private Sequence sequence;
        private Vector3 originalLocalPosition;
        private Vector3 originalLocalScale;
        private Quaternion originalLocalRotation;
        private bool cached;
        private bool completed;

        public bool IsPlaying => sequence != null && sequence.IsActive() && sequence.IsPlaying();
        public float Duration => delay + appearDuration + impactDuration + settleDuration;

        private void Awake()
        {
            CacheOriginalState();
        }

        public IEnumerator Play(PhasedBossController owner)
        {
            if (completed) yield break;

            CacheOriginalState();
            PrepareStartState();
            onSummonStart?.Invoke();

            sequence?.Kill(false);
            sequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            if (delay > 0f) sequence.AppendInterval(delay);
            sequence.AppendCallback(() => ODKSoundPlayback.Play(summonSound, transform.position));
            sequence.Append(visualRoot.DOLocalMove(originalLocalPosition, appearDuration).SetEase(appearEase));
            sequence.Join(visualRoot.DOLocalRotateQuaternion(originalLocalRotation, appearDuration).SetEase(Ease.OutCubic));

            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null) continue;
                Color color = originalColors[i];
                sequence.Join(renderer.DOFade(color.a, appearDuration * 0.72f));
                sequence.Join(renderer.DOColor(Color.Lerp(color, summonTint, 0.45f), appearDuration * 0.72f));
            }

            sequence.AppendCallback(() =>
            {
                owner?.ShakeCamera(cameraShake);
                onSummonImpact?.Invoke();
            });
            sequence.Append(visualRoot.DOPunchPosition(
                impactPunch,
                impactDuration + settleDuration,
                5,
                0.45f,
                false));

            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null) continue;
                sequence.Join(renderer.DOColor(originalColors[i], Mathf.Max(0.01f, flashDuration)));
            }

            sequence.OnComplete(() =>
            {
                completed = true;
                RestoreOriginalState();
                onSummonComplete?.Invoke();
            });

            yield return sequence.WaitForCompletion();
        }

        public void Cancel()
        {
            sequence?.Kill(false);
            sequence = null;
            RestoreOriginalState();
        }

        private void CacheOriginalState()
        {
            if (cached) return;
            if (visualRoot == null) visualRoot = transform;
            if (renderers == null || renderers.Length == 0)
                renderers = visualRoot.GetComponentsInChildren<SpriteRenderer>(true);

            originalLocalPosition = visualRoot.localPosition;
            originalLocalScale = visualRoot.localScale;
            originalLocalRotation = visualRoot.localRotation;
            originalColors.Clear();
            foreach (SpriteRenderer renderer in renderers)
                originalColors.Add(renderer != null ? renderer.color : Color.white);
            cached = true;
        }

        private void PrepareStartState()
        {
            visualRoot.localPosition = originalLocalPosition + startLocalOffset;
            visualRoot.localScale = originalLocalScale;
            visualRoot.localRotation = originalLocalRotation * Quaternion.Euler(0f, 0f, startRotation);

            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null) continue;
                Color color = Color.Lerp(originalColors[i], summonTint, 0.65f);
                color.a = startAlpha;
                renderer.color = color;
            }
        }

        private void RestoreOriginalState()
        {
            if (!cached || visualRoot == null) return;
            visualRoot.localPosition = originalLocalPosition;
            visualRoot.localScale = originalLocalScale;
            visualRoot.localRotation = originalLocalRotation;
            for (int i = 0; i < renderers.Length && i < originalColors.Count; i++)
            {
                if (renderers[i] != null) renderers[i].color = originalColors[i];
            }
        }

        private void OnDisable()
        {
            if (IsPlaying) Cancel();
        }

        private void OnDestroy()
        {
            sequence?.Kill(false);
        }
    }
}
