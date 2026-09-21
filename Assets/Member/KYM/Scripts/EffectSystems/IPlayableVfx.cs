namespace Member.KYM.Scripts.EffectSystems
{
    public interface IPlayableVfx
    {
        float Duration { get; }
        void PlayVfx();
        void StopVfx();
    }
}
