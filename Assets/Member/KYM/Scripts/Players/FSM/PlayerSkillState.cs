using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.Agents.FSM;
using Member.KYM.Scripts.CombatSystems.SkillSystems;
using UnityEngine;

namespace Member.KYM.Scripts.Players.FSM
{
    public class PlayerSkillState : AbstractPlayerState
    {
        private readonly ISkillModule _skillModule;
        private bool _isSkillEnd;
        
        public PlayerSkillState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
            _skillModule = agent.GetModule<ISkillModule>();
            Debug.Assert(_skillModule != null, "플레이어 스킬 상태는 스킬 모듈이 필요!!!");
        }

        public override void Enter()
        {
            _skillModule.OnCurrentSkillEnd += HandleSkillEnd;
            _isSkillEnd = false;
        }

        public override void Update()
        {
            base.Update();
            if (!_isSkillEnd)
                return;

            _player.ChangeState(
                _mover.IsGrounded
                    ? PlayerStateEnum.IDLE
                    : PlayerStateEnum.FALL);
        }

        public override void Exit()
        {
            _skillModule.OnCurrentSkillEnd -= HandleSkillEnd;
            base.Exit();
        }

        private void HandleSkillEnd() => _isSkillEnd = true;
    }
}
