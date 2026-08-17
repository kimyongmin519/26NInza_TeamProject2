using System;
using KimLIb.ModuleSystems;
using UnityEngine;

namespace KimLIb.AnimatorSystems
{
    public class AgentTrigger : MonoBehaviour, IModule
    {
        public event Action OnAnimationEnd;
        public event Action OnAnimationSpecialTrigger;
        private ModuleOwner _owner;
        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
        }
        
        public void InvokeAnimationEnd() => OnAnimationEnd?.Invoke();
        public void InvokeSpecialTrigger() => OnAnimationSpecialTrigger?.Invoke();
    }
}