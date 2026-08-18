using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.Agents.FSM;
using UnityEngine;

namespace Member.KYM.Scripts.Players.FSM
{
    public class PlayerJumpState : AbstractAirState
    {
        public PlayerJumpState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
            
        }

        public override void Enter()
        {
            base.Enter();
            
            _mover.StopImmediately(false, true);
            _mover.AddForceToAgent(Vector2.up * _player.JumpPower);
            
            _mover.OnVelocityChange += HandleVelocityChange;
        }

        private void HandleVelocityChange(Vector2 velocity)
        {
            if(velocity.y < 0)
                _player.ChangeState(PlayerStateEnum.FALL);
        }

        public override void Exit()
        {
            _mover.OnVelocityChange -= HandleVelocityChange;
            base.Exit();
        }
        
    }
}