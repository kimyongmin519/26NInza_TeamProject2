using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Member.KYM.Scripts.Enemies.Boss.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "EquipWeapon", story: "[Enemy] equiped weapon which [Index]", category: "Action", id: "8a9f32c2b327780bbc6ffd405dbba20c")]
    public partial class EquipWeaponAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractBoss> Enemy;
        [SerializeReference] public BlackboardVariable<int> Index;

        protected override Status OnStart()
        {
            if (Enemy?.Value == null || Index == null || Enemy.Value.WeaponModule == null)
                return Status.Failure;
            
            return Enemy.Value.WeaponModule.TryEquipWeapon(Index.Value)
                ? Status.Success
                : Status.Failure;
        }
    }
}

