using System;
using UnityEngine;
using UnityEngine.Events;

public class VolcanusTestHealth : MonoBehaviour, IDamageable
{
    [field: SerializeField] public float MaxHealth { get; private set; } = 300f;
    [field: SerializeField] public float CurrentHealth { get; private set; }
    [SerializeField] private bool invincible;
    [SerializeField] private UnityEvent onDeath;

    public event Action<float, float> OnHealthChanged;
    public bool IsDead { get; private set; }

    private void Awake()
    {
        CurrentHealth = Mathf.Max(1f, MaxHealth);
    }

    public void TakeDamage(DamageData damage)
    {
        if (invincible || IsDead || damage.Amount <= 0f) return;
        float previous = CurrentHealth;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage.Amount);
        OnHealthChanged?.Invoke(CurrentHealth, previous);
        if (CurrentHealth > 0f) return;
        IsDead = true;
        onDeath?.Invoke();
    }

    public void ResetHealth()
    {
        IsDead = false;
        float previous = CurrentHealth;
        CurrentHealth = Mathf.Max(1f, MaxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, previous);
    }

    public void SetMaxHealth(float value)
    {
        MaxHealth = Mathf.Max(1f, value);
        ResetHealth();
    }
}
