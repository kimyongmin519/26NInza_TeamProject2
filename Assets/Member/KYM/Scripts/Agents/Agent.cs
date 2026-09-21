using KimLIb.ModuleSystems;
using Member.ODK.Scripts;

namespace Member.KYM.Scripts.Agents
{
    public class Agent : ModuleOwner
    {
        public HealthModule HealthModule { get; private set; }
        public float NormalizeHealth => HealthModule == null
            ? 0f : HealthModule.CurrentHealth / HealthModule.MaxHealth;

        protected override void InitializeModules()
        {
            base.InitializeModules();

            HealthModule = GetModule<HealthModule>();
        }
    }
}
