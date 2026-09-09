using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.DamageSystems
{
    [CreateAssetMenu(fileName = "Damage data", menuName = "Combat/Damage data", order = 0)]
    public class DamageDataSO : ScriptableObject
    {
        [field:SerializeField] public float BaseDamageAmount { get; private set; }
        [field:SerializeField] public Vector2 KnockbackForce { get; private set; }
        [field:SerializeField] public float KnockbackDuration { get; private set; }
        [field:SerializeField] public float BaseKnockbackPower { get; private set; }
    }
}