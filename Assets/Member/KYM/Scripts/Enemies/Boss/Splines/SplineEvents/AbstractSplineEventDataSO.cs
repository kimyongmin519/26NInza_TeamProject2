using UnityEngine;

namespace Member.KYM.Scripts.Enemies.Boss.Splines.SplineEvents
{
    public abstract class AbstractSplineEventDataSO : ScriptableObject
    {
        public abstract void Handle(SplineEventContext context);
    }
}