using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    [RequireComponent(typeof(LineRenderer))]
    public class MoonTelegraphLine : MonoBehaviour
    {
        [SerializeField] private LineRenderer line;
        [SerializeField] private float pulseRate = 0.18f;
        [SerializeField] private float pulseScale = 0.45f;

        private Tween revealTween;
        private Tween pulseTween;
        private Vector3[] path = System.Array.Empty<Vector3>();
        private float progress;
        private float baseWidth;

        private void Awake()
        {
            if (line == null) line = GetComponent<LineRenderer>();
            baseWidth = Mathf.Max(0.001f, line.widthMultiplier);
            line.enabled = false;
        }

        public void Show(IReadOnlyList<Vector3> points, float duration)
        {
            if (points == null || points.Count < 2) return;
            path = new Vector3[points.Count];
            for (int i = 0; i < points.Count; i++) path[i] = points[i];

            KillTweens();
            line.enabled = true;
            line.widthMultiplier = baseWidth;
            progress = 0f;
            Draw();
            revealTween = DOTween.To(() => progress, value =>
                {
                    progress = value;
                    Draw();
                }, 1f, Mathf.Max(0.02f, duration))
                .SetEase(Ease.OutCubic)
                .SetTarget(this);
            pulseTween = DOTween.To(
                    () => line.widthMultiplier,
                    value => line.widthMultiplier = value,
                    baseWidth * Mathf.Max(0.1f, 1f - pulseScale),
                    Mathf.Max(0.05f, pulseRate))
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetTarget(this);
        }

        public void Hide()
        {
            KillTweens();
            if (line == null) return;
            line.widthMultiplier = baseWidth;
            line.enabled = false;
        }

        private void Draw()
        {
            if (path.Length < 2) return;
            line.positionCount = path.Length;
            float scaled = progress * (path.Length - 1);
            int segment = Mathf.Min(Mathf.FloorToInt(scaled), path.Length - 1);
            float rate = scaled - segment;
            for (int i = 0; i < path.Length; i++)
            {
                if (i <= segment) line.SetPosition(i, path[i]);
                else
                {
                    int start = Mathf.Min(segment, path.Length - 2);
                    line.SetPosition(i, Vector3.Lerp(path[start], path[start + 1], rate));
                }
            }
        }

        private void KillTweens()
        {
            revealTween?.Kill();
            pulseTween?.Kill();
            revealTween = null;
            pulseTween = null;
        }

        private void OnDisable() => KillTweens();
        private void OnDestroy() => KillTweens();
    }
}
