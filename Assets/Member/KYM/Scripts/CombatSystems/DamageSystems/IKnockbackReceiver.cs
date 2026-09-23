using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.DamageSystems
{
    public interface IKnockbackReceiver
    {
        void ApplyKnockback(Vector2 force);
    }
}
