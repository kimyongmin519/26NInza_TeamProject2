using System.Collections.Generic;
using Member.ODK._01_Script;
using Member.ODK.Scripts;
using UnityEngine;

namespace Member.YKJ.Bosses
{
    public static class MimicCombat
    {
        public static void DamageCircle(Vector2 center, float radius, LayerMask mask, float amount, Transform owner)
        {
            var damaged = new HashSet<IDamageable>();
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(center, radius, mask))
            {
                if (owner != null && hit.transform.IsChildOf(owner))
                    continue;
                IDamageable receiver = hit.GetComponentInParent<IDamageable>();
                if (receiver != null && damaged.Add(receiver))
                    receiver.TakeDamage(new DamageData(amount, DamageType.Special));
            }
        }
    }
}
