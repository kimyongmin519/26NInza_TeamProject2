using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.Agents.FSM;
using Member.ODK.Scripts.Enemys;
using UnityEngine;

namespace Member.KYM.Scripts.Players.FSM
{
    public abstract class AbstractEnemyState : AgentState
    {
        protected IMover _mover;
        protected EnemyController _player;
        protected const float INPUT_DEADLINE = 0.1f;
        
        public AbstractEnemyState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
            _mover = agent.GetModule<IMover>();
            Debug.Assert(_mover != null, "mover is null");
            _player = agent as EnemyController;
            Debug.Assert(_player != null, "agent is not enemy");
        }

        public override void Update()
        {
            base.Update();
        }
    }
}