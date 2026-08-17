using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.Agents.FSM;
using UnityEngine;

namespace Member.KYM.Scripts.Players.FSM
{
    public class PlayerMoveState : AbstractPlayerState
    {
        public PlayerMoveState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
            
        }

        public override void Update()
        {
            base.Update();

            if (Mathf.Abs(_player.PlayerInput.MoveDirX) < INPUT_DEADLINE)
            {
                _player.ChangeState(PlayerStateEnum.IDLE);
                return;
            }

            _mover.SetMovementX(_player.PlayerInput.MoveDirX);
        }
    }
}