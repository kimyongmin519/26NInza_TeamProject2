using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    [RequireComponent(typeof(LineRenderer))]
    public class MoonTelegraphLine : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private float revealDuration = 0.35f;
        [SerializeField] private float pulseAmount = 0.45f;
        [SerializeField] private float pulseDuration = 0.18f;
        [SerializeField] private Ease revealEase = Ease.OutCubic;

        private Vector3[] points = new Vector3[0];
        private Tween revealTween;
        private Tween pulseTween;
        private float revealProgress;
        private float baseWidth;

        public LineRenderer Renderer => lineRenderer;

        private void Awake()
        {
            EnsureRenderer();
        }

        public void Show(Vector3 start, Vector3 end, float duration = -1f)
        {
            Show(new[] { start, end }, duration);
        }

        public void Show(IReadOnlyList<Vector3> path, float duration = -1f)
        {
            EnsureRenderer();
            if (path == null || path.Count < 2) return;

            points = new Vector3[path.Count];
            for (int i = 0; i < path.Count; i++) points[i] = path[i];

            KillTweens();
            lineRenderer.enabled = true;
            revealProgress = 0f;
            DrawRevealedPath();

            float actualDuration = duration < 0f ? revealDuration : duration;
            revealTween = DOTween.To(
                    () => revealProgress,
                    value =>
                    {
                        revealProgress = value;
                        DrawRevealedPath();
                    },
                    1f,
                    Mathf.Max(0.01f, actualDuration))
                .SetEase(revealEase)
                .SetTarget(this);

            float minimumWidth = baseWidth * Mathf.Max(0.05f, 1f - pulseAmount);
            pulseTween = DOTween.To(
                    () => lineRenderer.widthMultiplier,
                    value => lineRenderer.widthMultiplier = value,
                    minimumWidth,
                    Mathf.Max(0.05f, pulseDuration))
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetTarget(this);
        }

        public void SetEndpoints(Vector3 start, Vector3 end)
        {
            if (points.Length != 2) points = new Vector3[2];
            points[0] = start;
            points[1] = end;
            DrawRevealedPath();
        }

        public void SetPath(IReadOnlyList<Vector3> path)
        {
            if (path == null || path.Count < 2) return;
            if (points.Length != path.Count) points = new Vector3[path.Count];
            for (int i = 0; i < path.Count; i++) points[i] = path[i];
            DrawRevealedPath();
        }

        public void Hide()
        {
            KillTweens();
            if (lineRenderer == null) return;
            lineRenderer.widthMultiplier = baseWidth;
            lineRenderer.enabled = false;
        }

        private void DrawRevealedPath()
        {
            if (lineRenderer == null || points.Length < 2) return;

            lineRenderer.positionCount = points.Length;
            float scaledProgress = revealProgress * (points.Length - 1);
            int completeSegment = Mathf.FloorToInt(scaledProgress);
            float segmentProgress = scaledProgress - completeSegment;

            for (int i = 0; i < points.Length; i++)
            {
                if (i <= completeSegment)
                {
                    lineRenderer.SetPosition(i, points[i]);
                    continue;
                }

                int fromIndex = Mathf.Clamp(completeSegment, 0, points.Length - 2);
                Vector3 end = Vector3.Lerp(points[fromIndex], points[fromIndex + 1], segmentProgress);
                lineRenderer.SetPosition(i, end);
            }
        }

        private void EnsureRenderer()
        {
            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
            baseWidth = Mathf.Max(0.001f, lineRenderer.widthMultiplier);
        }

        private void KillTweens()
        {
            revealTween?.Kill();
            pulseTween?.Kill();
            revealTween = null;
            pulseTween = null;
        }

        private void OnDisable()
        {
            KillTweens();
        }

        private void OnDestroy()
        {
            KillTweens();
        }
    }
}
