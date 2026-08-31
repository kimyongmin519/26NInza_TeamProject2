using Member.KYM.Scripts.Agents;
using UnityEngine;

namespace Member.KYM.Scripts.Enemies.Boss
{
    public abstract class AbstractBoss : Agent
    {
        [field:SerializeField] public BossDataSO BossData { get; private set; }
        
        
    }
}
