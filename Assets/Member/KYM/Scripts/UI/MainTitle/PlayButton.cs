using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace Member.KYM.Scripts.UI.MainTitle
{
    public class PlayButton : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera mainCam;
        [SerializeField] private CinemachineCamera titleCam;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        public void PressPlay()
        {
            mainCam.Priority = 10;
            titleCam.Priority = -10;
            
            _button.interactable = false;
        }
        
        
    }
}