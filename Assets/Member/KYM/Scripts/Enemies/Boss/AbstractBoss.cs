using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.CombatSystems.SkillSystems;
using Member.KYM.Scripts.CombatSystems.WeaponSystems;
using UnityEngine;

namespace Member.KYM.Scripts.Enemies.Boss
{
    public abstract class AbstractBoss : Agent
    {
        [field:SerializeField] public BossDataSO BossData { get; private set; }
        
        public IMover Mover { get; private set; }
        public IAnimateRenderer Renderer { get; private set; }
        public ISkillModule SkillModule { get; private set; }
        public IWeaponModule WeaponModule { get; private set; }
        public AgentSensor Sensor { get; private set; }

        protected override void InitializeModules()
        {
            base.InitializeModules();
            Mover = GetModule<IMover>();
            Renderer = GetModule<IAnimateRenderer>();
            Sensor = GetModule<AgentSensor>();
            SkillModule = GetModule<ISkillModule>();
            WeaponModule = GetModule<IWeaponModule>();
        }
    }
}
