using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Member.KYM.Scripts.Enemies.Boss.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "SetWeaponTarget",
        story: "Set [Enemy] weapon target to [TargetGameObject]",
        category: "Action",
        id: "fb4ed111352c4deab960b7a4dab69daf")]
    public partial class SetWeaponTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractBoss> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        protected override Status OnStart()
        {
            if (Enemy?.Value == null ||
                Enemy.Value.WeaponModule == null ||
                TargetGameObject?.Value == null)
            {
                return Status.Failure;
            }

            Enemy.Value.WeaponModule.SetTarget(TargetGameObject.Value);
            return Status.Success;
        }
    }
}
