using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.Agents.FSM;
using UnityEngine;

namespace Member.KYM.Scripts.Players.FSM
{
    public class PlayerHitState : AbstractPlayerState
    {
        private static float _hitRecoveryDuration = 0.4f;
        private float _recoveryTime;
        
        private readonly PlayerEyeTracker _eyes;

        public PlayerHitState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
            _eyes = _player.GetModule<PlayerEyeTracker>();
        }

        public override void Enter()
        {
            base.Enter();

            _recoveryTime = Time.time + _hitRecoveryDuration;
            _mover.SetMovementX(0f);
            _mover.CanManualMovement = false;
            _player.PlayerInput.AllInputLock(true);
            if (_eyes != null)
                _eyes.gameObject.SetActive(false);
        }

        public override void Update()
        {
            if (Time.time < _recoveryTime)
                return;

            _player.ChangeState(_mover.IsGrounded ? PlayerStateEnum.IDLE : PlayerStateEnum.FALL);
        }

        public override void Exit()
        {
            if (_eyes != null)
                _eyes.gameObject.SetActive(true);
            _player.PlayerInput.AllInputLock(false);
            _mover.CanManualMovement = true;
            base.Exit();
        }
    }
}
