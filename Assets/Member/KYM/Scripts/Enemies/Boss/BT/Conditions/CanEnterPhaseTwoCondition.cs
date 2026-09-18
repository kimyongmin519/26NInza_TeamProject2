using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Member.KYM.Scripts.Enemies.Boss.BT.Conditions
{
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Can Enter Phase Two",
        story: "[Enemy] health is below [HealthRatio]",
        category: "Conditions",
        id: "40e3da8188524f5f8e54724c751730bf")]
    public partial class CanEnterPhaseTwoCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<AbstractBoss> Enemy;
        [SerializeReference] public BlackboardVariable<float> HealthRatio;

        public override bool IsTrue()
        {
            AbstractBoss boss = Enemy?.Value;
            if (boss == null || boss.PhaseController == null)
                return false;

            float threshold = Mathf.Clamp01(HealthRatio?.Value ?? 0.5f);
            return boss.PhaseController.CurrentPhase == BossPhaseEnum.PHASE1 &&
                   boss.NormalizedHealth <= threshold;
        }
    }
}
