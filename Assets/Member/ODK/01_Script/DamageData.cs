using UnityEngine;

[System.Serializable]
public struct DamageData
{
    public float Damage;
    public Vector3 KnockbackForce;

    public DamageType DamageType;
    public CriticalType CriticalType;

    public DamageData(
        float damage,
        DamageType damageType,
        CriticalType criticalType = CriticalType.Normal,
        Vector3 knockbackForce = default)
    {
        Damage = damage;
        DamageType = damageType;
        CriticalType = criticalType;
        KnockbackForce = knockbackForce;
    }
}