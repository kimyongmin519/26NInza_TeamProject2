using System;
using Member.KYM.Scripts.Enemies.Boss.Splines;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Member.KYM.Scripts.Enemies.Boss.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "FindClosestSplinePath",
        story: "[Enemy] finds closest spline path and stores it in [Path]",
        category: "Action",
        id: "1a9c7918e4174d81a59341b1797db911")]
    public partial class FindClosestSplinePathAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractBoss> Enemy;
        [SerializeReference] public BlackboardVariable<SplinePath> Path;
        [SerializeReference] public BlackboardVariable<bool> ExcludeLastCompleted = new(true);

        protected override Status OnStart()
        {
            if (Enemy?.Value == null || Path == null)
                return Status.Failure;

            ISplineMover splineMover = Enemy.Value.GetModule<ISplineMover>();
            if (splineMover == null)
                return Status.Failure;

            if (!splineMover.TryGetClosestPath(
                    out SplinePath closestPath,
                    out _,
                    ExcludeLastCompleted?.Value ?? true))
            {
                Path.Value = null;
                return Status.Failure;
            }

            Path.Value = closestPath;
            return Status.Success;
        }
    }
}
