using UnityEngine;
using UnityEngine.UI;

namespace Member.ODK.Scripts.Tests
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "DamageButton")]
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
