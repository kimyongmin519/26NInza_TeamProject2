using KimLIb.AnimatorSystems;
using UnityEngine;

namespace Member.KYM.Scripts.Enemies.Boss.Splines.SplineEvents
{
    public readonly struct SplineJumpSettings
    {
        public float JumpHeight { get; }
        public float Duration { get; }
        public AnimParamSO EndAnimation { get; }

        public SplineJumpSettings(
            float jumpHeight,
            float duration,
            AnimParamSO endAnimation)
        {
            JumpHeight = Mathf.Max(0f, jumpHeight);
            Duration = Mathf.Max(0.1f, duration);
            EndAnimation = endAnimation;
        }
    }
}
