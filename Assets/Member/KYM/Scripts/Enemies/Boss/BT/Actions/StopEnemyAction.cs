using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Member.KYM.Scripts.Enemies.Boss.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "StopEnemy", story: "[Enemy] stop move x: [XValue] y: [YValue]", category: "Action", id: "0d4968dbf81ecbec5507aa3643aaa515")]
    public partial class StopEnemyAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractBoss> Enemy;
        [SerializeReference] public BlackboardVariable<bool> XValue;
        [SerializeReference] public BlackboardVariable<bool> YValue;

        protected override Status OnStart()
        {
            if (Enemy.Value == null || Enemy.Value.Mover == null)
                return Status.Failure;
            
            Enemy.Value.Mover.StopImmediately(XValue.Value, YValue.Value);
            return Status.Success;
        }
    }
}

