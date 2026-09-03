using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.Agents.FSM;
using Member.KYM.Scripts.CombatSystems.SkillSystems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Member.ODK.Scripts.Enemys
{
    public class EnemyController : Agent
    {
        //[SerializeField] private StateListSO stateList;
        public AgentSensor Sensor { get; private set; }
        public ISkillModule SkillModule { get; private set; }
        //private StateMachine _stateMachine;
        private void Start()
        {
            //ChangeState(PlayerStateEnum.IDLE);
        }
        protected override void InitializeModules()
        {
            base.InitializeModules();
            //_stateMachine = new StateMachine(this, stateList.states);
            Sensor = GetModule<AgentSensor>();
            SkillModule = GetModule<ISkillModule>();
        }

        protected override void AfterInitializeModules()
        {
            base.AfterInitializeModules();
        }
        protected virtual void Update()
        {
            //_stateMachine.UpdateMachine();
        }

        //public void ChangeState(PlayerStateEnum state) => _stateMachine.ChangeState((int)state);
    }

}
