using GGMLib.ObjectPool.Runtime;
using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.Projectiles
{
    [CreateAssetMenu(fileName = "Projectile data", menuName = "KimSO/Projectile data", order = 55)]
    public class ProjectileDataSO : ScriptableObject
    {
        [field:SerializeField] public float MoveSpeed { get; private set; }
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public PoolItemSO ImpactItem { get; private set; }
        [field:SerializeField] public Color ImpactColor { get; private set; } =  Color.white;
    }
}