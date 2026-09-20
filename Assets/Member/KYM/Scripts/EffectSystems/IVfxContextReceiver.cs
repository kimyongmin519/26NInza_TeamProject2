namespace Member.KYM.Scripts.EffectSystems
{
    public interface IVfxContextReceiver
    {
        void ApplyContext(in VfxSpawnContext context);
        void ResetContext();
    }
}
