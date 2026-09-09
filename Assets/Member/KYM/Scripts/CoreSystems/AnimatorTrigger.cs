using System;
using UnityEngine;

namespace Member.KYM.Scripts.CoreSystems
{
    public class AnimatorTrigger : MonoBehaviour
    {
        public event Action OnAnimationEnd;
        public void InvokeAnimationEnd() => OnAnimationEnd?.Invoke();
    }
}