using UnityEngine;

namespace Member.KYM.Scripts.Agents.FSM
{
    public abstract class AgentState
    {
        protected Agent _agent;
        protected readonly int _stateClipHash;

        protected IAnimateRenderer _renderer;

        public AgentState(Agent agent, int stateClipHash)
        {
            _agent = agent;
            _stateClipHash = stateClipHash;
            _renderer = _agent.GetModule<IAnimateRenderer>();
            Debug.Assert(_renderer != null, "renderer is null");
        }

        public virtual void Enter()
        {
            _renderer.PlayClip(_stateClipHash);
        }
        
        public virtual void Update() {}

        public virtual void Exit() {}
    }
}