using System;
using System.Threading;
using KimLIb.EventSystem;
using Member.KYM.Scripts.CoreSystems.Events;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Member.KYM.Scripts.Enemies.Boss.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Play Timeline",
        story: "Play timeline [TimelineId] through [TimelineChannel] and wait up to [Timeout] seconds",
        category: "Action",
        id: "6e858886042647f3951845d0c7878c30")]
    public partial class PlayTimelineAction : Action
    {
        [SerializeReference] public BlackboardVariable<EventChannelSO> TimelineChannel;
        [SerializeReference] public BlackboardVariable<int> TimelineId;
        [SerializeReference] public BlackboardVariable<float> Timeout = new(30f);

        private static int _nextRequestId;

        private EventChannelSO _channel;
        private int _requestId;
        private float _elapsedTime;
        private bool _requestSent;
        private bool _hasResult;
        private bool _completed;

        protected override Status OnStart()
        {
            _channel = TimelineChannel?.Value;
            if (_channel == null || TimelineId == null)
                return Status.Failure;

            _requestId = Interlocked.Increment(ref _nextRequestId);
            _elapsedTime = 0f;
            _requestSent = true;
            _hasResult = false;
            _completed = false;

            _channel.AddListener<TimelinePlaybackEndedEvent>(HandlePlaybackEnded);
            _channel.RaiseEvent(
                new TimelinePlayRequestEvent(TimelineId.Value, _requestId));

            return GetCurrentStatus();
        }

        protected override Status OnUpdate()
        {
            if (_hasResult)
                return GetCurrentStatus();

            float timeout = Timeout?.Value ?? 0f;
            if (timeout > 0f)
            {
                _elapsedTime += Time.unscaledDeltaTime;
                if (_elapsedTime >= timeout)
                    return Status.Failure;
            }

            return Status.Running;
        }

        protected override void OnEnd()
        {
            if (_channel == null)
                return;

            _channel.RemoveListener<TimelinePlaybackEndedEvent>(HandlePlaybackEnded);

            if (_requestSent && !_hasResult)
            {
                _channel.RaiseEvent(
                    new TimelineStopRequestEvent(TimelineId.Value, _requestId));
            }

            _requestSent = false;
        }

        private void HandlePlaybackEnded(TimelinePlaybackEndedEvent evt)
        {
            if (evt == null ||
                evt.RequestId != _requestId ||
                evt.TimelineId != TimelineId.Value)
            {
                return;
            }

            _hasResult = true;
            _completed = evt.Completed;
        }

        private Status GetCurrentStatus()
        {
            if (!_hasResult)
                return Status.Running;

            return _completed ? Status.Success : Status.Failure;
        }
    }
}
