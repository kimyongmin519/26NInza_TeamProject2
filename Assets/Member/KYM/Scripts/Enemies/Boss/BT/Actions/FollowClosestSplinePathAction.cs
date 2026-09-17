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
        name: "FollowClosestSplinePath",
        story: "[Enemy] follows closest spline path, gravity: [UseGravity]",
        category: "Action",
        id: "b10e55af1a174f0893a150d692593927")]
    public partial class FollowClosestSplinePathAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractBoss> Enemy;
        [SerializeReference] public BlackboardVariable<bool> UseGravity = new(false);

        private ISplineMover _splineMover;

        protected override Status OnStart()
        {
            if (Enemy?.Value == null)
                return Status.Failure;

            _splineMover = Enemy.Value.GetModule<ISplineMover>();
            if (_splineMover == null)
                return Status.Failure;

            return _splineMover.BeginClosestPath(UseGravity?.Value ?? false)
                ? Status.Running
                : Status.Failure;
        }

        protected override Status OnUpdate()
        {
            if (_splineMover == null)
                return Status.Failure;

            if (_splineMover.IsFollowing)
                return Status.Running;

            return _splineMover.IsCompleted
                ? Status.Success
                : Status.Failure;
        }

        protected override void OnEnd()
        {
            if (_splineMover != null && _splineMover.IsFollowing)
                _splineMover.CancelFollow();
        }
    }
}
