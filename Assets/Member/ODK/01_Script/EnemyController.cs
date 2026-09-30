using System;
using System.Collections.Generic;
using KimLIb.ModuleSystems;
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
        protected override void Awake()
        {
            _moduleDict = new Dictionary<Type, IModule>();
            foreach (IModule module in GetComponentsInChildren<IModule>())
            {
                if (module == null) continue;
                Type type = module.GetType();
                if (_moduleDict.ContainsKey(type))
                {
                    Component duplicate = module as Component;
                    Debug.LogWarning($"[{name}] 중복 모듈 무시: {type.Name} ({(duplicate != null ? duplicate.name : "?")})", duplicate);
                    continue;
                }
                _moduleDict.Add(type, module);
            }
            InitializeModules();
            AfterInitializeModules();
        }

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
