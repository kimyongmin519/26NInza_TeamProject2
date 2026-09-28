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
            if (line != null)
            {
                if (baseWidth >= 0f) line.widthMultiplier = baseWidth;
                line.startColor = pathColor;
                line.endColor = pathColor;
                line.enabled = false;
            }
        }

        public void Show(Vector3 start, Vector3 end, float duration, float worldWidth = -1f)
        {
            if (line == null) return;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startColor = pathColor;
            line.endColor = pathColor;
            line.widthMultiplier = worldWidth > 0f ? worldWidth : pathWidth;
            line.enabled = true;
            pulseTween?.Kill();
            if (worldWidth > 0f) return;
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
