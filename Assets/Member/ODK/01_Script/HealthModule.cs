using KimLIb.ModuleSystems;
using System;
using UnityEngine;

public class HealthModule : IModule
{
    public float MaxHealth { get; private set; }
    public float CurrentHealth { get; private set; }

    public bool IsDead { get; private set; }

    public Action<float, float> OnHealthChanged;
    public ModuleOwner owner;
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

        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public void Initialize(ModuleOwner owner)
    {
        this.owner = owner;

        CurrentHealth = MaxHealth;

        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }
}