using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Member.KYM.Scripts.Enemies.Boss.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "UnEquipWeapon", story: "[Boss] unequip currentweapon", category: "Action", id: "5b966749d2b6d58616de68c80be91c62")]
    public partial class UnEquipWeaponAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractBoss> Boss;

        protected override Status OnStart()
        {
            if (Boss?.Value == null || Boss.Value.WeaponModule == null)
                return Status.Failure;

            Boss.Value.WeaponModule.UnequipCurrentWeapon();
            return Status.Success;
        }
    }
}

