using System;
using Member.KYM.Scripts.Enemies.Boss.BT;
using Member.KYM.Scripts.Enemies.Boss.BT.Channels;
using Member.ODK._01_Script;
using Unity.Behavior;
using UnityEngine;

namespace Member.KYM.Scripts.Enemies.Boss
{
    public class BTAgentBoss : AbstractBoss, IDamageable
    {
        public BehaviorGraphAgent BTAgent { get; private set; }
        public StateChannel StateChannel { get; private set; }
        private bool _deathStateRequested;

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
            HealthModule.OnDeath += HandleDeath;
        }

        private void Start()
        {
            SetVariableValue<AbstractBoss>(BtVar.Boss, this);

            if (BTAgent.GetVariable(BtVar.StateChannel, out BlackboardVariable<StateChannel> channelVar))
            {
                StateChannel = channelVar.Value;
            }
            if (PhaseController != null)
                SetVariableValue(BtVar.CurrentPhase, PhaseController.CurrentPhase);
            if (HealthModule != null && HealthModule.IsDead)
                HandleDeath();
        }

        private void HandleDeath()
        {
            if (_deathStateRequested || StateChannel == null)
                return;

            _deathStateRequested = true;
            StateChannel.SendEventMessage(BossStateEnum.DEATH);
        }

        private void HandlePhaseChanged(BossPhaseEnum phase)
        {
            SetVariableValue(BtVar.CurrentPhase, phase);
        }

        private void OnDestroy()
        {
            if (PhaseController != null)
                PhaseController.OnPhaseChanged -= HandlePhaseChanged;
            if (HealthModule != null)
                HealthModule.OnDeath -= HandleDeath;
        }

        public void TakeDamage(DamageData damage)
        {
            if (HealthModule == null || HealthModule.IsDead || damage.Amount <= 0f)
                return;

            bool wasInvincible = HealthModule.IsInvisible;
            HealthModule.ApplyDamage(damage);

            if (wasInvincible || HealthModule.IsDead)
                return;

            StateChannel.SendEventMessage(BossStateEnum.HIT);
            
            ApplyKnockback(damage);
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
