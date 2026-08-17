using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.Agents.FSM;
using UnityEngine;

namespace Member.KYM.Scripts.Players.FSM
{
    public abstract class AbstractPlayerState : AgentState
    {
        protected IMover _mover;
        protected PlayerController _player;
        protected const float INPUT_DEADLINE = 0.1f;
        
        public AbstractPlayerState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
            _mover = agent.GetModule<IMover>();
            Debug.Assert(_mover != null, "mover is null");
            _player = agent as PlayerController;
            Debug.Assert(_player != null, "agent is not player");
        }

        public override void Update()
        {
            base.Update();
            _renderer.FlipController(_player.PlayerInput.MoveDirX);
        }
    }
}