using System;
using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.CombatSystems.SkillSystems;
using Member.KYM.Scripts.CombatSystems.WeaponSystems;
using UnityEngine;

namespace Member.KYM.Scripts.Enemies.Boss
{
    public abstract class AbstractBoss : Agent
    {
        [field:SerializeField] public BossDataSO BossData { get; private set; }

        public float CurrentHealth { get; private set; }
        public float NormalizedHealth => BossData == null || BossData.MaxHealth <= 0f
            ? 0f
            : CurrentHealth / BossData.MaxHealth;

        public event Action<float> OnHealthChanged;
        
        public IMover Mover { get; private set; }
        public IAnimateRenderer Renderer { get; private set; }
        public ISkillModule SkillModule { get; private set; }
        public IWeaponModule WeaponModule { get; private set; }
        public AgentSensor Sensor { get; private set; }
        public BossPhaseController PhaseController { get; private set; }

        protected override void InitializeModules()
        {
            base.InitializeModules();
            CurrentHealth = BossData != null ? BossData.MaxHealth : 0f;
            Mover = GetModule<IMover>();
            Renderer = GetModule<IAnimateRenderer>();
            Sensor = GetModule<AgentSensor>();
            SkillModule = GetModule<ISkillModule>();
            WeaponModule = GetModule<IWeaponModule>();
            PhaseController = GetModule<BossPhaseController>();
        }

        protected void ApplyDamage(float damageAmount)
        {
            if (damageAmount <= 0f || CurrentHealth <= 0f)
                return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damageAmount);
            OnHealthChanged?.Invoke(NormalizedHealth);
        }
    }
}
