using System;
using Member.KYM.Scripts.Agents;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Member.KYM.Scripts.Enemies.Boss.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "FindTarget", story: "[Enemy] find [TargetGameObject]", category: "Action", id: "e49c29370f0d45e6a461eb249941777b")]
    public partial class FindTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractBoss> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;
        
        protected override Status OnStart()
        {
            //이미 적을 감지했거나 값들이 잘못들어가 있으면 Fail
            if (Enemy.Value == null)
                return Status.Failure;

            if (TargetGameObject.Value != null)
                return Status.Success;

            AgentSensor sensor = Enemy.Value.Sensor;

            Collider2D findtarget = null;
            bool isFinding = sensor.IsTargetInRange(999f, out findtarget);
            if(!isFinding) return Status.Failure;
            
            TargetGameObject.Value = findtarget.gameObject;
            return TargetGameObject.Value == null ? Status.Failure : Status.Success;
        }
    }
}

