using System;
using Member.KYM.Scripts.Agents;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Member.KYM.Scripts.Enemies.Boss.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "RandomShortMove",
        story: "[Enemy] moves randomly left or right for [MinDuration] to [MaxDuration]",
        category: "Action",
        id: "2079ba71706b435aa81bc80a9f8a6d47")]
    public partial class RandomShortMoveAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractBoss> Enemy;
        [SerializeReference] public BlackboardVariable<float> MinDuration = new(0.15f);
        [SerializeReference] public BlackboardVariable<float> MaxDuration = new(0.45f);
        [SerializeReference] public BlackboardVariable<float> MoveInput = new(1f);

        private IMover _mover;
        private float _endTime;

        protected override Status OnStart()
        {
            if (Enemy?.Value == null || Enemy.Value.Mover == null)
                return Status.Failure;

            _mover = Enemy.Value.Mover;

            float minDuration = Mathf.Max(0f, MinDuration?.Value ?? 0f);
            float maxDuration = Mathf.Max(minDuration, MaxDuration?.Value ?? minDuration);
            float duration = UnityEngine.Random.Range(minDuration, maxDuration);
            float direction = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            float moveInput = Mathf.Clamp01(Mathf.Abs(MoveInput?.Value ?? 1f));

            _endTime = Time.time + duration;
            _mover.SetMovementX(direction * moveInput);
            Enemy.Value.Renderer?.FlipController(direction);

            return duration <= 0f ? Status.Success : Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (_mover == null)
                return Status.Failure;

            return Time.time >= _endTime
                ? Status.Success
                : Status.Running;
        }

        protected override void OnEnd()
        {
            if (_mover == null)
                return;

            _mover.SetMovementX(0f);
            _mover.StopImmediately(true, false);
        }
    }
}
