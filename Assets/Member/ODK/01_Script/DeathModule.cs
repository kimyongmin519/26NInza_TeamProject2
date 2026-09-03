using KimLIb.ModuleSystems;
using System;
using UnityEngine;

namespace Member.ODK.Scripts
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "DeathModule")]
    public class DeathModule : MonoBehaviour, IModule
    {
        public bool IsDead;
        public Action OnDeath { get; set; }
        private ModuleOwner owner;

        public void Initialize(ModuleOwner owner)
        {
            this.owner = owner;
        }
        public void ActiveDeath()
        {
            OnDeath?.Invoke();
        }
        [ContextMenu("Revive")]
        public void Revive()
        {
            IsDead = false;
            HealthModule hm = owner.GetModule<HealthModule>();
            hm.SetMaxHealth(hm.MaxHealth, true);
        }
    }
}
