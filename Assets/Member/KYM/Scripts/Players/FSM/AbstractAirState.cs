using Member.KYM.Scripts.Agents;

namespace Member.KYM.Scripts.Players.FSM
{
    public abstract class AbstractAirState : AbstractPlayerState
    {
        protected AbstractAirState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
            
        }

        public override void Update()
        {
            base.Update();
            
            float xInput = _player.PlayerInput.MoveDirX;
            _mover.SetMovementX(xInput);
        }
    }
}