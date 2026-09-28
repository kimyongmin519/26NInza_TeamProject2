using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.DamageSystems
{
    public class DirectDamageCaster : AbstractDamageCaster
    {
        public override bool CastDamage(Collider2D hitCollider, Vector2 hitPoint, Vector2 hitNormal)
        {
            if (hitCollider == null ||
                contactFilter.IsFilteringLayerMask(hitCollider.gameObject) ||
                contactFilter.IsFilteringDepth(hitCollider.gameObject) ||
                contactFilter.IsFilteringTrigger(hitCollider) ||
                contactFilter.IsFilteringNormalAngle(hitNormal))
            {
                return false;
            }

            return TryApplyDamage(hitCollider, hitPoint, hitNormal);
        }
    }
}
