namespace Member.KYM.Scripts.CoreSystems.MapSystem
{
    public interface IMapDirectTarget
    {
        int SignalId { get; }
        void PlayDirect();
    }
}
