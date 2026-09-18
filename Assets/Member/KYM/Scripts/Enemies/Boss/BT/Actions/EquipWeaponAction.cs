using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Member.KYM.Scripts.Enemies.Boss.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "EquipWeapon",
        story: "[Enemy] equips weapon [Index] with target [TargetGameObject]",
        category: "Action",
        id: "8a9f32c2b327780bbc6ffd405dbba20c")]
    public partial class EquipWeaponAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractBoss> Enemy;
        [SerializeReference] public BlackboardVariable<int> Index;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        protected override Status OnStart()
        {
            if (Enemy?.Value == null || Enemy.Value.WeaponModule == null)
            {
                return Status.Failure;
            }

            if (TargetGameObject?.Value != null)
                Enemy.Value.WeaponModule.SetTarget(TargetGameObject.Value);

            return Enemy.Value.WeaponModule.TryEquipWeapon(Index.Value)
                ? Status.Success
                : Status.Failure;
        }
    }
}

