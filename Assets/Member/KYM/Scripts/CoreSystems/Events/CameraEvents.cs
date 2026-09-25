using KimLIb.EventSystem;
using UnityEngine;

namespace Member.KYM.Scripts.CoreSystems.Events
{
    public static class CameraEvents
    {
        public static CameraShakeEvent CameraShake = new CameraShakeEvent();
    }

    public class CameraShakeEvent : GameEvent
    {
        public float Power;
        public float Duration;

        public CameraShakeEvent InitData(float power, float duration)
        {
            Power = power;
            Duration = duration;
            return this;
        }
    }
}