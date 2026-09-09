using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.Agents.FSM;
using UnityEngine;

namespace Member.KYM.Scripts.Players.FSM
{
    public class PlayerCrouchState : AbstractPlayerState
    {
        public PlayerCrouchState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
        }

        public override void Enter()
        {
            base.Enter();
            _mover.SetMovementX(0f);
            _mover.StopImmediately(true, false);
            _player.SetCrouching(true);
        }

        public override void Update()
        {
            base.Update();

            if (_player.PlayerInput.MoveDirY < -INPUT_DEADLINE)
                return;

            PlayerStateEnum nextState = Mathf.Abs(_player.PlayerInput.MoveDirX) > INPUT_DEADLINE
                ? PlayerStateEnum.MOVE
                : PlayerStateEnum.IDLE;

            _player.ChangeState(nextState);
        }

        public override void Exit()
        {
            _player.SetCrouching(false);
            base.Exit();
        }
    }
}
