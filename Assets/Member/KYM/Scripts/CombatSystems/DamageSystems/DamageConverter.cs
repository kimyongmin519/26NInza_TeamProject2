using Member.KYM.Scripts.Enemies.Boss;
using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.DamageSystems
{
    public static class DamageConverter
    {
        public static int DamageToPlayerHitDamage(float damage)
        {
            int convertDamage = Mathf.RoundToInt(damage / damage);
            convertDamage = convertDamage < 1 ? 1 : convertDamage;
            return convertDamage;
        }
    }
}