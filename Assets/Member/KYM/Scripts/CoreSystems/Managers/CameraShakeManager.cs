using System;
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
        private CinemachineImpulseManager.ImpulseEvent _activeImpulse;
        private object _activeSignal;
        private int _lastShakeFrame = -1;
        private float _lastShakePower;
        private float _lastShakeDuration;
        
        private void Awake()
        {
            _impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        private void Start()
        {
            ConfigureCameraListeners();
            cameraChannel.AddListener<CameraShakeEvent>(HandleCameraShake);
        }

        private void OnDestroy()
        {
            CancelActiveShake();
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

            // 같은 프레임의 여러 적중은 가장 강한 요청 하나로 처리한다.
            if (_lastShakeFrame == Time.frameCount &&
                (evt.Power < _lastShakePower ||
                 (evt.Power == _lastShakePower && evt.Duration <= _lastShakeDuration)))
            {
                return;
            }

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

            // 새 타격은 이전 타격의 남은 흔들림보다 우선한다.
            // 다른 시스템의 임펄스까지 지우지 않고 이 매니저의 신호만 취소한다.
            CancelActiveShake();
            _activeImpulse = impulse.CreateAndReturnEvent(
                transform.position, _impulseSource.DefaultVelocity * evt.Power);
            _activeSignal = _activeImpulse?.SignalSource;
            _lastShakeFrame = Time.frameCount;
            _lastShakePower = evt.Power;
            _lastShakeDuration = evt.Duration;
        }

        private void CancelActiveShake()
        {
            // Cinemachine이 만료된 이벤트 객체를 재사용했으면 건드리지 않는다.
            if (_activeImpulse != null && _activeSignal != null &&
                ReferenceEquals(_activeImpulse.SignalSource, _activeSignal))
            {
                float now = CinemachineImpulseManager.Instance.CurrentTime;
                // 시작 프레임에도 양수 길이를 남겨 만료 판정이 가능하게 한다.
                _activeImpulse.Cancel(Mathf.Max(now, _activeImpulse.StartTime + 0.0001f), true);
            }

            _activeImpulse = null;
            _activeSignal = null;
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
