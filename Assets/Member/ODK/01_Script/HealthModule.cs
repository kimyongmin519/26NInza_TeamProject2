using Member.ODK.Scripts.UI;
using KimLIb.ModuleSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Member.ODK.Scripts
{
    public class HealthModule : MonoBehaviour, IModule
    {
        [field:SerializeField] public float MaxHealth { get; private set; } = 1000;
        [field: SerializeField] public float CurrentHealth { get; private set; } = 1000;

        public bool IsDead { get; protected set; }

        public Action<float, float> OnHealthChanged;
        public ModuleOwner owner;


        protected Dictionary<Type, IHealthModuleExtra> _moduleDict;

        protected virtual void Awake()
        {
            _moduleDict = GetComponentsInChildren<IHealthModuleExtra>().ToDictionary(module => module.GetType(), module => module);
            InitializeHealthModuleExtra();
        }

        protected virtual void InitializeHealthModuleExtra()
        {
            foreach (IHealthModuleExtra module in _moduleDict.Values)
            {
                module.Setting(this);
            }
        }
        public void ApplyDamage(DamageData damage)
        {
            if (IsDead)
                return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - damage.Amount);

            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

            if (CurrentHealth <= 0)
            {
                IsDead = true;
                owner.GetModule<DeathModule>().ActiveDeath();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f)
                return;

            CurrentHealth = Mathf.Min(
                CurrentHealth + amount,
                MaxHealth
            );

            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void SetMaxHealth(float maxHealth, bool fullHeal = true)
        {
            MaxHealth = Mathf.Max(1f, maxHealth);

            if (fullHeal)
                CurrentHealth = MaxHealth;
            else
                CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
            IsDead = false;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void Initialize(ModuleOwner owner)
        {
            this.owner = owner;

            CurrentHealth = MaxHealth;

            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

        }
    }
}
