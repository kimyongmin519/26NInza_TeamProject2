using KimLIb.ModuleSystems;
using UnityEngine;

namespace Member.KYM.Scripts.Agents
{
    public class AgentRenderer : MonoBehaviour, IAnimateRenderer, IModule
    {
        [field: SerializeField] public float FacingDirection { get; private set; } = 1f;
        public Animator Animator { get; private set; }
        private ModuleOwner _owner;
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            Animator = GetComponent<Animator>();
        }
        public void PlayClip(int clipHash)
        {
            Animator.Play(clipHash);
        }

        public void FlipController(float xMoveDirection)
        {
            if(Mathf.Abs(FacingDirection + xMoveDirection) < 0.5f)
                Flip();
        }

        private void Flip()
        {
            FacingDirection *= -1;
            float targetYRotation = FacingDirection > 0 ? 0 : 180f;
            transform.rotation = Quaternion.Euler(0, targetYRotation, 0);
        }
    }
}