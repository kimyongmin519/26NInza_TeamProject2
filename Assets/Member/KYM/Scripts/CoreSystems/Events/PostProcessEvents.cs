using KimLIb.EventSystem;
using Member.KYM.Scripts.CoreSystems.PostProcessSystem;

namespace Member.KYM.Scripts.CoreSystems.Events
{
    public static class PostProcessEvents
    {
        public static readonly PostProcessRequestEvent HurtVignetteEvent = new(PostProcessType.HurtVignette);
        public static readonly PostProcessRequestEvent ParryImpactEvent = new(PostProcessType.ParryImpact);
    }

    public sealed class PostProcessRequestEvent : GameEvent
    {
        public PostProcessType Type { get; }
        public PostProcessRequest Request { get; private set; }

        public PostProcessRequestEvent(PostProcessType type)
        {
            Type = type;
        }

        public PostProcessRequestEvent Play()
        {
            Request = new PostProcessRequest(PostProcessCommand.Play);
            return this;
        }

        public PostProcessRequestEvent Stop()
        {
            Request = new PostProcessRequest(PostProcessCommand.Stop);
            return this;
        }
    }
}
