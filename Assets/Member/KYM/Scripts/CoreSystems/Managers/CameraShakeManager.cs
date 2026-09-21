using System;
using KimLIb.EventSystem;
using Unity.Cinemachine;
using UnityEngine;

namespace Member.KYM.Scripts.CoreSystems.Managers
{
    public class CameraShakeManager : MonoBehaviour
    {
        [field:SerializeField] public EventChannelSO cameraChannel;
        private CinemachineCamera _cineCam;
        
        private void Awake()
        {
            _cineCam = GetComponent<CinemachineCamera>();
        }
        
        
    }
}