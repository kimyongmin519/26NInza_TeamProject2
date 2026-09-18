using System;
using KimLIb.ModuleSystems;
using Member.KYM.Scripts.Enemies.Boss.BT;
using UnityEngine;

namespace Member.KYM.Scripts.Enemies.Boss
{
    public class BossPhaseController : MonoBehaviour, IModule
    {
        [SerializeField] private BossPhaseEnum initialPhase = BossPhaseEnum.PHASE1;

        public BossPhaseEnum CurrentPhase { get; private set; }
        public event Action<BossPhaseEnum> OnPhaseChanged;

        public void Initialize(ModuleOwner owner)
        {
            CurrentPhase = initialPhase;
        }

        public bool TryChangePhase(BossPhaseEnum nextPhase)
        {
            if (CurrentPhase == nextPhase)
                return false;

            if (CurrentPhase != BossPhaseEnum.PHASE1 ||
                nextPhase != BossPhaseEnum.PHASE2)
                return false;

            ChangePhase(nextPhase);
            return true;
        }

        private void ChangePhase(BossPhaseEnum nextPhase)
        {
            CurrentPhase = nextPhase;
            OnPhaseChanged?.Invoke(CurrentPhase);
        }
    }
}
