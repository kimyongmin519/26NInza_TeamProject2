
using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.Agents.FSM;
using Member.KYM.Scripts.CombatSystems.SkillSystems;
using Member.ODK.Scripts.Enemys.Modules;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Member.ODK.Scripts.Enemys
{
    public class FlyingBossControler : EnemyController
    {
        [SerializeField] private Transform target;
        private FlyingMover mover;

        protected override void InitializeModules()
        {
            base.InitializeModules();
            mover = GetModule<FlyingMover>();

        }

        protected override void Update()
        {
            base.Update();
            
            Vector2 wow = Vector2.Distance(transform.position,target.position) * (target.position - transform.position).normalized;
            mover.SetForceToAgent(wow);
        }
    }

}
