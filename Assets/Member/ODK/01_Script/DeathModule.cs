using KimLIb.ModuleSystems;
using System;
using UnityEngine;

public class DeathModule : IModule
{
    public bool IsDead;
    public Action OnDeath { get; set; }
    private ModuleOwner owner;

    public void Initialize(ModuleOwner owner)
    {
        this.owner = owner;
    }
    public void ActiveDeath()
    {
        OnDeath?.Invoke();
    }
}
