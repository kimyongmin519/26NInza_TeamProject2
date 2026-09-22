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
        
        private void Awake()
        {
            _impulseSource = GetComponent<CinemachineImpulseSource>();
            
            cameraChannel.AddListener<CameraShakeEvent>(HandleCameraShake);
        }

        private void OnDestroy()
        {
            cameraChannel.RemoveListener<CameraShakeEvent>(HandleCameraShake);
        }

        private void HandleCameraShake(CameraShakeEvent evt)
        {
            _impulseSource.ImpulseDefinition.ImpulseDuration = evt.Duration;
            _impulseSource.GenerateImpulse(evt.Power);
        }
    }
}