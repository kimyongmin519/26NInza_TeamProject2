using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Member.KYM.Scripts.Enemies.Boss.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Set Boss Invincible",
        story: "Set [Boss] invincible to [Invincible]",
        category: "Action",
        id: "f7d64682775d4393ba46eb2ad28a3b1e")]
    public partial class SetBossInvincibleAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractBoss> Boss;
        [SerializeReference] public BlackboardVariable<bool> Invincible;

        protected override Status OnStart()
        {
            AbstractBoss boss = Boss?.Value;
            if (boss == null || boss.HealthModule == null || Invincible == null)
                return Status.Failure;

            boss.HealthModule.SettingInvisibleTime(
                Invincible.Value ? float.PositiveInfinity : 0f);
            return Status.Success;
        }
    }
}
