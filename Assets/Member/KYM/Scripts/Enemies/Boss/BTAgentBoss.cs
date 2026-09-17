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

        private void Start()
        {
            SetVariableValue<AbstractBoss>(BtVar.Boss, this);
        }

        public void TakeDamage(DamageData damage)
        {
            
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