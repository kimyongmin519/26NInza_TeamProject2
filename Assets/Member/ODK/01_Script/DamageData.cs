using UnityEngine;

[System.Serializable]
public struct DamageData
{
    public float Amount;
    public Vector3 KnockbackForce;

    public DamageType DamageType;
    public CriticalType CriticalType;

    public DamageData(
        float amount,
        DamageType damageType,
        CriticalType criticalType = CriticalType.Normal,
        Vector3 knockbackForce = default)
    {
        Amount = amount;
        DamageType = damageType;
        CriticalType = criticalType;
        KnockbackForce = knockbackForce;
    }
}