using KimLIb.EventSystem;
using Member.KYM.Scripts.CoreSystems.Events;
using Unity.Cinemachine;
using UnityEngine;

namespace Member.KYM.Scripts.CoreSystems.Managers
{
    public class CameraShakeManager : MonoBehaviour
    {
        [field:SerializeField] public EventChannelSO cameraChannel;
        private CinemachineImpulseSource _impulseSource;
        
        private void Awake()
        {
            _impulseSource = GetComponent<CinemachineImpulseSource>();

            ConfigureCameraListeners();
            cameraChannel.AddListener<CameraShakeEvent>(HandleCameraShake);
        }

        private void OnDestroy()
        {
            cameraChannel.RemoveListener<CameraShakeEvent>(HandleCameraShake);
        }

        private void HandleCameraShake(CameraShakeEvent evt)
        {
            if (evt == null || evt.Power <= 0f || evt.Duration <= 0f || _impulseSource == null)
                return;

            // Cinemachine keeps a reference to the definition while an impulse is active.
            // Each request therefore needs its own definition so a later Duration cannot
            // change the shape of a shake that is already playing.
            CinemachineImpulseDefinition source = _impulseSource.ImpulseDefinition;
            if (source == null)
                return;

            CinemachineImpulseDefinition impulse = new CinemachineImpulseDefinition
            {
                ImpulseChannel = source.ImpulseChannel,
                // A short Rumble has too many zero crossings to sample reliably.
                ImpulseShape = source.ImpulseShape == CinemachineImpulseDefinition.ImpulseShapes.Rumble
                    && evt.Duration <= 0.25f
                    ? CinemachineImpulseDefinition.ImpulseShapes.Recoil
                    : source.ImpulseShape,
                CustomImpulseShape = source.CustomImpulseShape,
                ImpulseDuration = evt.Duration,
                ImpulseType = source.ImpulseType,
                DissipationRate = source.DissipationRate,
                RawSignal = source.RawSignal,
                AmplitudeGain = source.AmplitudeGain,
                FrequencyGain = source.FrequencyGain,
                RepeatMode = source.RepeatMode,
                Randomize = source.Randomize,
                TimeEnvelope = source.TimeEnvelope,
                ImpactRadius = source.ImpactRadius,
                DirectionMode = source.DirectionMode,
                DissipationMode = source.DissipationMode,
                DissipationDistance = source.DissipationDistance,
                PropagationSpeed = source.PropagationSpeed
            };

            impulse.CreateEvent(transform.position, _impulseSource.DefaultVelocity * evt.Power);
        }

        private void ConfigureCameraListeners()
        {
            if (_impulseSource?.ImpulseDefinition == null)
                return;

            int channel = _impulseSource.ImpulseDefinition.ImpulseChannel;
            CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (CinemachineCamera camera in cameras)
            {
                if (camera.gameObject.scene != gameObject.scene)
                    continue;

                CinemachineImpulseListener listener = camera.GetComponent<CinemachineImpulseListener>();
                if (listener == null)
                {
                    listener = camera.gameObject.AddComponent<CinemachineImpulseListener>();
                    listener.Gain = 1f;
                    listener.UseCameraSpace = true;
                }

                listener.ChannelMask |= channel;
                listener.SignalCombinationMode =
                    CinemachineImpulseListener.SignalCombinationModes.UseLargest;
            }
        }
    }
}
