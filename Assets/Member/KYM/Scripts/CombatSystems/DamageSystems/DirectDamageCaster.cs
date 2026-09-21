using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.DamageSystems
{
    public class DirectDamageCaster : AbstractDamageCaster
    {
        public override bool CastDamage(Collider2D hitCollider, Vector2 hitPoint, Vector2 hitNormal)
        {
            return TryApplyDamage(hitCollider, hitPoint, hitNormal);
        }
    }
}
