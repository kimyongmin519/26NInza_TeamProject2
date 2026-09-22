using Member.KYM.Scripts.Agents;
using UnityEngine;

namespace Member.KYM.Scripts.Players.FSM
{
    public class PlayerDeathState : AbstractPlayerState
    {
        private readonly PlayerEyeTracker _eyes;

        public PlayerDeathState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
            _eyes = _player.GetModule<PlayerEyeTracker>();
        }

        public override void Enter()
        {
            base.Enter();
            _player.SetCrouching(false);
            _mover.SetMovementX(0f);
            _mover.StopImmediately(true, true);
            _mover.CanManualMovement = false;
            _player.PlayerInput.AllInputLock(true);

            if (_eyes != null)
                _eyes.gameObject.SetActive(false);
        }
        public override void Exit()
        {
            if (_eyes != null)
                _eyes.gameObject.SetActive(true);

            _mover.CanManualMovement = true;
            base.Exit();
        }
    }
}
