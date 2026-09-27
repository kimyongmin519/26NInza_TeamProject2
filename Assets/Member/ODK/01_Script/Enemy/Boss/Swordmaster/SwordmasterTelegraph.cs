using DG.Tweening;
using GGMLib.ObjectPool.Runtime;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Swordmaster
{
    [RequireComponent(typeof(LineRenderer))]
    public class SwordmasterTelegraph : AbstractMonoPoolable
    {
        [SerializeField] private Color pathColor = new Color(1f, 1f, 1f, 0.32f);
        [SerializeField] private float pathWidth = 0.08f;
        private LineRenderer line;
        private Tween pulseTween;

        private float baseWidth = -1f;

        private void Awake()
        {
            line = GetComponent<LineRenderer>();
            if (line != null) baseWidth = line.widthMultiplier;
        }

        public override void ResetItem()
        {
            pulseTween?.Kill();
            pulseTween = null;
            if (line == null) line = GetComponent<LineRenderer>();
            if (line != null && baseWidth >= 0f) line.widthMultiplier = baseWidth;
        }

        public void Show(Vector3 start, Vector3 end, float duration)
        {
            if (line == null) return;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startColor = pathColor;
            line.endColor = pathColor;
            line.widthMultiplier = pathWidth;
            line.enabled = true;
            pulseTween?.Kill();
            pulseTween = DOTween.To(
                    () => line.widthMultiplier,
                    value => line.widthMultiplier = value,
                    pathWidth * 0.45f,
                    Mathf.Max(0.05f, duration * 0.25f)
                )
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetTarget(this);
        }

        public void Hide()
        {
            pulseTween?.Kill();
            pulseTween = null;
            if (line != null) line.enabled = false;
        }

        private void OnDisable() => pulseTween?.Kill();
        private void OnDestroy() => pulseTween?.Kill();
    }
}
