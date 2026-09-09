using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.Agents.FSM;

namespace Member.KYM.Scripts.Players.FSM
{
    public class PlayerFallState : AbstractAirState
    {
        public PlayerFallState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
            
        }

        public override void Update()
        {
            base.Update();
            if (_mover.IsGrounded)
            {
                LandingGround();
            }
        }

        private void LandingGround()
        {
            _player.ResetJumpCount();
            _player.ChangeState(PlayerStateEnum.IDLE);
        }
    }
}