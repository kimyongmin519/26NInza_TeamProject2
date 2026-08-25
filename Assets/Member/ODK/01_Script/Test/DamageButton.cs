using UnityEngine;
using UnityEngine.UI;

public class DamageButton : MonoBehaviour
{

    [SerializeField] private HealthModule enemy;
    [SerializeField] private float dmage = 50;
   


    public void ApplyDamage()
    {
        DamageData damage= new DamageData();
        damage.Amount = dmage;
        enemy.ApplyDamage(damage);
    }

}
