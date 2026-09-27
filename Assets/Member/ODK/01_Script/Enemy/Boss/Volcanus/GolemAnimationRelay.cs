using System;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public class GolemAnimationRelay : MonoBehaviour
    {
        public event Action OnAttackStart;

        public void AttackStart()
        {
            OnAttackStart?.Invoke();
        }

        public void AttackStartEffectObject()
        {
        }
    }
}
