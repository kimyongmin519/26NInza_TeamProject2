using Member.KYM.Scripts.Agents;

namespace Member.KYM.Scripts.Players.FSM
{
    public class PlayerGrappleState : AbstractPlayerState
    {
        public PlayerGrappleState(Agent agent, int stateClipHash)
            : base(agent, stateClipHash)
        {
        }

        public override void Enter()
        {
            base.Enter();
            _mover.SetMovementX(0f);
        }

        public override void Update()
        {
            base.Update();
            _mover.SetMovementX(0f);
        }
    }
}
