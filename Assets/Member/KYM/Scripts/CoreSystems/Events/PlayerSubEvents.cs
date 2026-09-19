using KimLIb.EventSystem;
using Member.ODK.Scripts;

namespace Member.KYM.Scripts.CoreSystems.Events
{
    public static class PlayerSubEvents
    {
        public static PlayerHealthSubEvent PlayerHealthSubEvent = new PlayerHealthSubEvent();
    }

    public class PlayerHealthSubEvent : GameEvent
    {
        public HealthModule PlayerHealthModule { get; private set; }

        public PlayerHealthSubEvent InitData(HealthModule healthModule)
        {
            PlayerHealthModule = healthModule;

            return this;
        }
    }
}