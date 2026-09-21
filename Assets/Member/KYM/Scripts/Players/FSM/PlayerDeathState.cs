using Member.KYM.Scripts.Agents;
using UnityEngine;

namespace Member.KYM.Scripts.Players.FSM
{
    public class PlayerDeathState : AbstractPlayerState
    {
        private readonly GameObject _eyes;

        public PlayerDeathState(Agent agent, int stateClipHash)
            : base(agent, stateClipHash)
        {
            PlayerEyeTracker eyeTracker =
                agent.GetComponentInChildren<PlayerEyeTracker>(true);
            _eyes = eyeTracker != null ? eyeTracker.gameObject : null;
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
                _eyes.SetActive(false);
        }

        public override void Update()
        {
        }

        public override void Exit()
        {
            if (_eyes != null)
                _eyes.SetActive(true);

            _mover.CanManualMovement = true;
            base.Exit();
        }
    }
}
