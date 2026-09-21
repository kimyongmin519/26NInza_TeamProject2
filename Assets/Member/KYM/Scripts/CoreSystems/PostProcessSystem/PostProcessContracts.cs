namespace Member.KYM.Scripts.CoreSystems.PostProcessSystem
{
    public enum PostProcessType
    {
        HurtVignette
    }

    public enum PostProcessCommand
    {
        Play,
        Stop
    }

    public readonly struct PostProcessRequest
    {
        public PostProcessCommand Command { get; }

        public PostProcessRequest(PostProcessCommand command)
        {
            Command = command;
        }
    }

    public interface IPostProcessEffect
    {
        PostProcessType Type { get; }
        void Handle(PostProcessRequest request);
    }
}
