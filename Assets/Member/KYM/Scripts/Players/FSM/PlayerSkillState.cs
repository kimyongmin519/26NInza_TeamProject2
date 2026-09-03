using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.Agents.FSM;
using UnityEngine;

namespace Member.KYM.Scripts.Players.FSM
{
    public class PlayerSkillState : AbstractPlayerState
    {
        private readonly SkillModule _skillModule;
        private bool _isSkillEnd;
        
        public PlayerSkillState(Agent agent, int stateClipHash) : base(agent, stateClipHash)
        {
            _skillModule = agent.GetModule<SkillModule>();
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
            if(_isSkillEnd)
                _player.ChangeState(PlayerStateEnum.IDLE);
        }

        public override void Exit()
        {
            _skillModule.OnCurrentSkillEnd -= HandleSkillEnd;
            base.Exit();
        }

        private void HandleSkillEnd() => _isSkillEnd = true;
    }
}