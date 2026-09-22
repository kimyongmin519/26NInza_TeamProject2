using Member.ODK.Scripts;
using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.DamageSystems
{
    [CreateAssetMenu(fileName = "Damage data", menuName = "KimSO/Combat/Damage data", order = 0)]
    public class DamageDataSO : ScriptableObject
    {
        [field:SerializeField] public DamageType DamageType { get; private set; }
        [field:SerializeField] public float BaseDamageAmount { get; private set; }
        [field:SerializeField] public Vector2 KnockbackForce { get; private set; }
    }
}
