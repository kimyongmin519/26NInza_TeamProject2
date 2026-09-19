using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Member.KYM.Scripts.Enemies.Boss.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Change Boss Phase",
        story: "Change [Enemy] phase to [Phase]",
        category: "Action",
        id: "d61a88e99f1a410a9c165cc29235d4ae")]
    public partial class ChangeBossPhaseAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractBoss> Enemy;
        [SerializeReference] public BlackboardVariable<BossPhaseEnum> Phase;

        protected override Status OnStart()
        {
            AbstractBoss boss = Enemy?.Value;
            if (boss == null || boss.PhaseController == null || Phase == null)
                return Status.Failure;

            if (boss.PhaseController.CurrentPhase == Phase.Value)
                return Status.Success;

            return boss.PhaseController.TryChangePhase(Phase.Value)
                ? Status.Success
                : Status.Failure;
        }
    }
}
