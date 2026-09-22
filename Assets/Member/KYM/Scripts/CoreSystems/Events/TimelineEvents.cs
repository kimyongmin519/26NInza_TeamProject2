using KimLIb.EventSystem;

namespace Member.KYM.Scripts.CoreSystems.Events
{
    public sealed class TimelinePlayRequestEvent : GameEvent
    {
        public int TimelineId { get; }
        public int RequestId { get; }

        public TimelinePlayRequestEvent(int timelineId, int requestId)
        {
            TimelineId = timelineId;
            RequestId = requestId;
        }
    }

    public sealed class TimelineStopRequestEvent : GameEvent
    {
        public int TimelineId { get; }
        public int RequestId { get; }

        public TimelineStopRequestEvent(int timelineId, int requestId)
        {
            TimelineId = timelineId;
            RequestId = requestId;
        }
    }

    public sealed class TimelinePlaybackEndedEvent : GameEvent
    {
        public int TimelineId { get; }
        public int RequestId { get; }
        public bool Completed { get; }

        public TimelinePlaybackEndedEvent(
            int timelineId,
            int requestId,
            bool completed)
        {
            TimelineId = timelineId;
            RequestId = requestId;
            Completed = completed;
        }
    }
}
