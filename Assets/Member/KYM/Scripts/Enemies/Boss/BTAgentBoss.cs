using System;
using Member.KYM.Scripts.Enemies.Boss.BT;
using Member.ODK._01_Script;
using Unity.Behavior;
using UnityEngine;

namespace Member.KYM.Scripts.Enemies.Boss
{
    public class BTAgentBoss : AbstractBoss, IDamageable
    {
        public BehaviorGraphAgent BTAgent { get; private set; }

        protected override void InitializeModules()
        {
            base.InitializeModules();
            BTAgent = GetComponent<BehaviorGraphAgent>();
        }

        protected override void AfterInitializeModules()
        {
            base.AfterInitializeModules();
            
            PhaseController.OnPhaseChanged += HandlePhaseChanged;
            HealthModule.SetMaxHealth(BossData.MaxHealth);
        }

        private void Start()
        {
            SetVariableValue<AbstractBoss>(BtVar.Boss, this);

            if (PhaseController == null)
                return;

            SetVariableValue(BtVar.CurrentPhase, PhaseController.CurrentPhase);
        }

        private void HandlePhaseChanged(BossPhaseEnum phase)
        {
            SetVariableValue(BtVar.CurrentPhase, phase);
        }

        private void OnDestroy()
        {
            if (PhaseController != null)
                PhaseController.OnPhaseChanged -= HandlePhaseChanged;
        }

        public void TakeDamage(DamageData damage)
        {
            HealthModule?.ApplyDamage(damage);
        }
        
        public void SetVariableValue<T>(string variableName, T value)
        {
            Debug.Assert(!string.IsNullOrEmpty(variableName));

            if (BTAgent.GetVariable<T>(variableName, out BlackboardVariable<T> variable))
            {
                variable.Value = value;
            }
            else
            {
                Debug.LogError($"Variable {variableName} not found");
            }
        }
    }
}
