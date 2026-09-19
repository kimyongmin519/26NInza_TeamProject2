using UnityEngine;
using UnityEngine.UI;

namespace Member.ODK.Scripts.Tests
{
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
}
