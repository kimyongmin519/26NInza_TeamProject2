using UnityEngine;

namespace Member.KYM.Scripts.Agents
{
    public interface IAnimateRenderer
    {
        Animator Animator { get; }
        float FacingDirection { get; }
        void PlayClip(int clipHash);
        void FlipController(float xMoveDirection);
    }
}