using System;
using Member.KYM.Scripts.Enemies.Boss.Splines;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Member.KYM.Scripts.Enemies.Boss.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "FollowSplinePath", story: "[Enemy] follows [Path] from closest end, gravity: [UseGravity]", category: "Action", id: "47a8f90aa7914df6a43da02fd3472a45")]
    public partial class FollowSplinePathAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractBoss> Enemy;
        [SerializeReference] public BlackboardVariable<SplinePath> Path;
        [SerializeReference] public BlackboardVariable<bool> UseGravity = new(false);

        private ISplineMover _splineMover;

        protected override Status OnStart()
        {
            if (Enemy?.Value == null || Path?.Value == null)
                return Status.Failure;

            _splineMover = Enemy.Value.GetModule<ISplineMover>();
            if (_splineMover == null || Enemy.Value.Mover?.RigidBody == null)
                return Status.Failure;

            Rigidbody2D rigidBody = Enemy.Value.Mover.RigidBody;
            float bodyToFeetOffsetY =
                SplineAgentPositionUtility.GetBodyToFeetOffsetY(rigidBody);
            Vector2 feetPosition = SplineAgentPositionUtility.GetFeetPosition(
                rigidBody,
                bodyToFeetOffsetY);
            float startT = Path.Value.GetClosestEndT(feetPosition);
            bool reverse = startT >= 1f;
            
            return _splineMover.BeginPath(Path.Value, reverse, UseGravity?.Value ?? false) ? Status.Running : Status.Failure;
        }

        protected override Status OnUpdate()
        {
            if (_splineMover.IsFollowing)
                return Status.Running;
            
            return _splineMover.IsCompleted ? Status.Success : Status.Failure;
        }

        protected override void OnEnd()
        {
            if (_splineMover != null && _splineMover.IsFollowing)
                _splineMover.CancelFollow();
        }
    }
}
