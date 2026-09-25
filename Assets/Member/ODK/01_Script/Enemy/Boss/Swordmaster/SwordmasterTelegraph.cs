using DG.Tweening;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Swordmaster
{
    [RequireComponent(typeof(LineRenderer))]
    public class SwordmasterTelegraph : MonoBehaviour
    {
        private LineRenderer line;
        private Tween pulseTween;

        private void Awake()
        {
            line = GetComponent<LineRenderer>();
        }

        public void Show(Vector3 start, Vector3 end, float duration)
        {
            if (line == null) return;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.enabled = true;
            pulseTween?.Kill();
            pulseTween = DOTween.To(
                    () => line.widthMultiplier,
                    value => line.widthMultiplier = value,
                    0.3f,
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

        private void OnDestroy() => pulseTween?.Kill();
    }
}
