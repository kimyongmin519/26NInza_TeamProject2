using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Random = UnityEngine.Random;

namespace Member.KYM.Scripts.Enemies.Boss.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "ReturnRandomInt", story: "[Min] to [Max] out [Value]", category: "Action", id: "bc559c3b8774897149de0ab0568c895a")]
    public partial class ReturnRandomIntAction : Action
    {
        [SerializeReference] public BlackboardVariable<int> Min;
        [SerializeReference] public BlackboardVariable<int> Max;
        [SerializeReference] public BlackboardVariable<int> Value;
        
        
        
        protected override Status OnStart()
        {
            Value.Value = Random.Range(Min, Max + 1);
            return Status.Success;
        }
        
    }
}

