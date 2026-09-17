using KimLIb.AnimatorSystems;
using UnityEngine;

namespace Member.KYM.Scripts.Enemies.Boss.Splines.SplineEvents
{
    [CreateAssetMenu(
        fileName = "Spline Jump Event",
        menuName = "KimSO/Boss/Spline Events/Jump")]
    public class SplineJumpEventDataSO : AbstractSplineEventDataSO
    {
        [Header("점프 설정")]
        [SerializeField, Min(0f)] private float jumpHeight = 1.25f;
        [SerializeField, Min(0.1f)] private float duration = 0.8f;

        [Header("선택 사항")]
        [SerializeField] private AnimParamSO jumpAnimation;
        [SerializeField] private AnimParamSO landingAnimation;

        public override void Handle(SplineEventContext context)
        {
            if (context.SplineMover == null)
                return;

            var settings = new SplineJumpSettings(
                jumpHeight,
                duration,
                landingAnimation);

            bool started = context.SplineMover.BeginSplineJump(
                context.KnotIndex,
                settings);

            if (started && jumpAnimation != null && context.Boss?.Renderer != null)
                context.Boss.Renderer.PlayClip(jumpAnimation.ParamHash);
        }
    }
}
