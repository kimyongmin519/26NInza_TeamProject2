using KimLIb.AnimatorSystems;
using UnityEngine;

namespace Member.KYM.Scripts.EffectSystems
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class SpriteAnimationVfx : MonoBehaviour, IPlayableVfx
    {
        [field: SerializeField, Min(0.01f)] public float Duration { get; private set; } = 0.35f;
        [SerializeField] private Animator animator;
        [SerializeField] private AnimParamSO playParam;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
        }

        public void PlayVfx()
        {
            if (animator == null)
                return;

            animator.enabled = true;
            animator.Play(playParam.ParamHash, 0, 0f);
            animator.Update(0f);
        }

        public void StopVfx()
        {
            if (animator != null)
                animator.enabled = false;
        }

        private void OnValidate()
        {
            Duration = Mathf.Max(0.01f, Duration);
        }
    }
}
