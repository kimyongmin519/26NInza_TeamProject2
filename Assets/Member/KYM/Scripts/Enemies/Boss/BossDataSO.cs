using UnityEngine;

namespace Member.KYM.Scripts.Enemies.Boss
{
    [CreateAssetMenu(fileName = "Boss data", menuName = "KimSO/Boss/Boss data", order = 0)]
    public class BossDataSO : ScriptableObject
    {
        [field:SerializeField] public float MaxHealth { get; private set; }
    }
}