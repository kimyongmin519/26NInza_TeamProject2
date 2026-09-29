using KimLIb.ModuleSystems;
using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.Players;
using Member.ODK._01_Script;
using Member.ODK.Scripts;
using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.DamageSystems
{
    public static class PlayerDamageFeedback
    {
        public static bool Apply(ModuleOwner attacker, IDamageable target, DamageData damage, Vector2 hitPoint)
        {
            if (target == null)
                return false;

            HealthModule health = FindHealth(target);
            float previousHealth = health != null ? health.CurrentHealth : 0f;
            target.TakeDamage(damage);
            return Report(attacker, target, previousHealth, hitPoint);
        }

        public static bool Report(ModuleOwner attacker, IDamageable target, float previousHealth, Vector2 hitPoint)
        {
            if (target == null)
                return false;

            HealthModule health = FindHealth(target);
            return Report(attacker, previousHealth, health != null ? health.CurrentHealth : previousHealth - 1f, hitPoint);
        }

        public static bool Report(ModuleOwner attacker, float previousHealth, float currentHealth, Vector2 hitPoint)
        {
            if (attacker is not PlayerController player || currentHealth >= previousHealth)
                return false;

            player.PlayEnemyHitFeedback(hitPoint);
            return true;
        }

        public static float ReadHealth(IDamageable target)
        {
            HealthModule health = FindHealth(target);
            return health != null ? health.CurrentHealth : 0f;
        }

        private static HealthModule FindHealth(IDamageable target)
        {
            return target is Agent agent
                ? agent.HealthModule
                : (target as Component)?.GetComponentInChildren<HealthModule>(true);
        }
    }
}
