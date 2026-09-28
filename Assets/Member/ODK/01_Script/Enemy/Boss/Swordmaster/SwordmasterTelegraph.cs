using DG.Tweening;
using GGMLib.ObjectPool.Runtime;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Swordmaster
{
    [RequireComponent(typeof(LineRenderer))]
    public class SwordmasterTelegraph : AbstractMonoPoolable
    {
        [SerializeField] private Color pathColor = new Color(1f, 1f, 1f, 0.32f);
        [SerializeField, Min(0.01f)] private float pathWidth = 0.08f;
        [SerializeField, Min(0.01f)] private float maximumWidth = 0.5f;
        [SerializeField, Range(0.05f, 0.5f)] private float revealDurationRatio = 0.2f;
        [SerializeField, Range(0.1f, 1f)] private float dimmedAlphaRatio = 0.55f;
        private LineRenderer line;
        private Tween pulseTween;
        private Tween revealTween;

        private float baseWidth = -1f;

        private void Awake()
        {
            line = GetComponent<LineRenderer>();
            if (line != null) baseWidth = line.widthMultiplier;
        }

        public override void ResetItem()
        {
            pulseTween?.Kill();
            revealTween?.Kill();
            pulseTween = null;
            revealTween = null;
            if (line == null) line = GetComponent<LineRenderer>();
            if (line != null)
            {
                line.widthMultiplier = GetSafeWidth(baseWidth >= 0f ? baseWidth : pathWidth);
                line.startColor = pathColor;
                line.endColor = pathColor;
                line.positionCount = 2;
                line.SetPosition(0, Vector3.zero);
                line.SetPosition(1, Vector3.zero);
                line.enabled = false;
            }
            transform.localScale = Vector3.one;
        }

        public void Show(Vector3 start, Vector3 end, float duration, float worldWidth = -1f)
        {
            if (line == null) return;
            pulseTween?.Kill();
            revealTween?.Kill();
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, start);
            line.widthMultiplier = GetSafeWidth(worldWidth > 0f ? worldWidth : pathWidth);
            SetAlpha(pathColor.a * dimmedAlphaRatio);
            line.enabled = true;

            float revealDuration = Mathf.Clamp(
                duration * revealDurationRatio,
                0.04f,
                0.14f
            );
            float revealProgress = 0f;
            revealTween = DOTween.To(
                    () => revealProgress,
                    value =>
                    {
                        revealProgress = value;
                        line.SetPosition(1, Vector3.LerpUnclamped(start, end, value));
                    },
                    1f,
                    revealDuration
                )
                .SetEase(Ease.OutCubic)
                .SetTarget(this)
                .OnComplete(() => revealTween = null);

            float alpha = pathColor.a * dimmedAlphaRatio;
            pulseTween = DOTween.To(
                    () => alpha,
                    value =>
                    {
                        alpha = value;
                        SetAlpha(value);
                    },
                    pathColor.a,
                    Mathf.Max(0.08f, duration * 0.22f)
                )
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetTarget(this);
        }

        public void Hide()
        {
            pulseTween?.Kill();
            revealTween?.Kill();
            pulseTween = null;
            revealTween = null;
            if (line != null) line.enabled = false;
        }

        private float GetSafeWidth(float requestedWidth)
        {
            return Mathf.Clamp(requestedWidth, 0.01f, Mathf.Max(0.01f, maximumWidth));
        }

        private void SetAlpha(float alpha)
        {
            if (line == null) return;
            Color color = pathColor;
            color.a = Mathf.Clamp01(alpha);
            line.startColor = color;
            line.endColor = color;
        }

        private void OnDisable()
        {
            pulseTween?.Kill();
            revealTween?.Kill();
        }

        private void OnDestroy()
        {
            pulseTween?.Kill();
            revealTween?.Kill();
        }
    }
}
