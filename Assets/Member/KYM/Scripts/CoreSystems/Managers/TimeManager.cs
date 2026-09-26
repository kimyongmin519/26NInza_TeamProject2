using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Member.KYM.Scripts.CoreSystems.Managers
{
    public class TimeManager : MonoSingleton<TimeManager>
    {
        private float _currentTime = 1f;
        private float _beforeTime;

        public void StopTimer()
        {
            SetBeforeTime(_currentTime);
            _currentTime = 0f;
        }

        public async void StopTimer(float duration)
        {
            SetBeforeTime(_currentTime);
            
            _currentTime = 0f;
            await Task.Delay(TimeSpan.FromSeconds(duration));
            _currentTime = _beforeTime;
        }
        
        public async void StartTimer()
        {
            SetBeforeTime(_currentTime);
            _currentTime = _beforeTime;
        }
        
        public void SetBeforeTime(float time) => _beforeTime = time;

        private void Update()
        {
            Time.timeScale = _currentTime;
        }
    }
}