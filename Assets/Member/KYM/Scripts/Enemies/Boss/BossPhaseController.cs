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

        /*public bool TryStartPhaseTwoTransition()
        {
            if (CurrentPhase != BossPhaseEnum.PHASE1)
                return false;

            ChangePhase(BossPhaseEnum.TRANSITION);
            return true;
        }

        public bool TryCompletePhaseTwoTransition()
        {
            if (CurrentPhase != BossPhaseEnum.TRANSITION)
                return false;

            ChangePhase(BossPhaseEnum.PHASE2);
            return true;
        }*/

        private void ChangePhase(BossPhaseEnum nextPhase)
        {
            CurrentPhase = nextPhase;
            OnPhaseChanged?.Invoke(CurrentPhase);
        }
    }
}
