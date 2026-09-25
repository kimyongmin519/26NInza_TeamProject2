using GGMLib.CoreLibrary;
using KimLIb.EventSystem;
using Member.KYM.Scripts.CombatSystems.DamageSystems;
using Member.KYM.Scripts.CoreSystems.Events;
using Member.KYM.Scripts.Players;
using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.Feedback
{
    [DisallowMultipleComponent]
    public class CameraShakeFeedback : AbstractFeedback
    {
        [SerializeField] private EventChannelSO cameraChannel;

        [Header("카메라 흔들림")]
        [SerializeField, Min(0f)] private float shakePower = 1f;
        [SerializeField, Min(0f)] private float shakeDuration = 0.15f;

        private void OnValidate()
        {
            shakePower = Mathf.Max(0f, shakePower);
            shakeDuration = Mathf.Max(0f, shakeDuration);
        }

        public override void PlayFeedback()
        {
            if (cameraChannel == null)
                return;

            cameraChannel.RaiseEvent(
                new CameraShakeEvent().InitData(shakePower, shakeDuration));
        }
    }
}
