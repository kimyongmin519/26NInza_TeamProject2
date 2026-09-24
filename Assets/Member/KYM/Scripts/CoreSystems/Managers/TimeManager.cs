using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Member.KYM.Scripts.CoreSystems.Managers
{
    public class TimeManager : MonoSingleton<TimeManager>
    {
        private float _currentTime = 1f;

        public void StopTime() => _currentTime = 0f;

        public async void StopTime(float duration)
        {
            float beforeTime = _currentTime;
            
            _currentTime = 0f;
            await Task.Delay(TimeSpan.FromSeconds(duration));
            _currentTime = beforeTime;
        }

        private void Update()
        {
            Time.timeScale = _currentTime;
        }
    }
}