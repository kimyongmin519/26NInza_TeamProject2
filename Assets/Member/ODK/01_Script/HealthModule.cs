using KimLIb.ModuleSystems;
using System;
using UnityEngine;

namespace Member.ODK.Scripts
{
    public class HealthModule : MonoBehaviour, IModule
    {
        [field: SerializeField] public float MaxHealth { get; private set; } = 1000;
        [field: SerializeField] public float CurrentHealth { get; private set; } = 1000;

        public float InvisibleTime { get; private set; }
        private ModuleOwner owner;

        public bool IsDead { get; protected set; }
        public bool IsInvisible => InvisibleTime > 0f;

        public Action OnInvisibleHited;
        public Action<float, float> OnHealthChanged;
        public Action OnDeath { get; set; }

        public void Initialize(ModuleOwner owner)
        {
            this.owner = owner;

            IsDead = false;
            InvisibleTime = 0f;
            CurrentHealth = MaxHealth;

            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        private void Update()
        {
            if (InvisibleTime <= 0f)
                return;

            InvisibleTime = Mathf.Max(0f, InvisibleTime - Time.deltaTime);
        }

        public void ActiveDeath()
        {
            if (IsDead)
                return;

            IsDead = true;
            OnDeath?.Invoke();
        }

        [ContextMenu("Revive")]
        public void Revive()
        {
            IsDead = false;
            CurrentHealth = MaxHealth;
            InvisibleTime = 0f;

            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void SettingInvisibleTime(float value)
        {
            InvisibleTime = Mathf.Max(0f, value);
        }

        public void AddInvisibleTime(float value)
        {
            InvisibleTime = Mathf.Max(0f, InvisibleTime + value);
        }

        public void ApplyDamage(DamageData damage)
        {
            if (IsDead || damage.Amount <= 0f)
                return;

            if (IsInvisible)
            {
                OnInvisibleHited?.Invoke();
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage.Amount);

            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

            if (CurrentHealth <= 0f)
                ActiveDeath();
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f)
                return;

            CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);

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
    }
}