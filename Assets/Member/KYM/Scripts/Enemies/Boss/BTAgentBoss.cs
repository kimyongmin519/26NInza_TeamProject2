using Unity.Behavior;

namespace Member.KYM.Scripts.Enemies.Boss
{
    public class BTAgentBoss : AbstractBoss, IDamageable
    {
        public BehaviorGraphAgent BTAgent { get; private set; }

        protected override void InitializeModules()
        {
            base.InitializeModules();
            BTAgent = GetComponent<BehaviorGraphAgent>();
            
        }

        public void TakeDamage(DamageData damage)
        {
            
        }
        
    }
}