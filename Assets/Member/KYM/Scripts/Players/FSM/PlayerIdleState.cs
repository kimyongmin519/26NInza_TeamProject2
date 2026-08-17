using System.Numerics;
using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.Agents.FSM;
using UnityEngine;

namespace Member.KYM.Scripts.Players.FSM
{
    public class PlayerIdleState : AbstractPlayerState
    {
        public PlayerIdleState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
            
        }
        
        public override void Enter()
        {
            base.Enter();
            _mover.SetMovementX(0);
            _mover.StopImmediately(true,false);
        }

        public override void Update()
        {
            base.Update();
            
            if (Mathf.Abs(_player.PlayerInput.MoveDirX) > INPUT_DEADLINE)
            {
                _player.ChangeState(PlayerStateEnum.MOVE);
            }
        }
    }
}