using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Member.KYM.Scripts.CoreSystems.Managers
{
    public class TimeManager : MonoSingleton<TimeManager>
    {
        private float _currentTime = 1f;
        private float _beforeTime = 1f;
        private bool _manualStopped;
        private int _timedStops;
        private int _pauseScopes;

        public IDisposable Pause()
        {
            SaveRunningTime();
            _pauseScopes++;
            _currentTime = 0f;
            Time.timeScale = 0f;
            return new PauseScope(this);
        }

        public void StopTimer()
        {
            SaveRunningTime();
            _manualStopped = true;
            _currentTime = 0f;
        }

        public async void StopTimer(float duration)
        {
            SaveRunningTime();
            _timedStops++;
            _currentTime = 0f;
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(0f, duration)), destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                _timedStops--;
                if (this != null)
                    RestoreRunningTime();
            }
        }
        
        public void StartTimer()
        {
            _manualStopped = false;
            RestoreRunningTime();
        }

        private void SaveRunningTime()
        {
            if (_currentTime > 0f)
                _beforeTime = _currentTime;
        }

        private void RestoreRunningTime()
        {
            if (!_manualStopped && _timedStops == 0 && _pauseScopes == 0)
                _currentTime = _beforeTime;
        }

        private void ReleasePause()
        {
            if (_pauseScopes == 0)
                return;

            _pauseScopes--;
            RestoreRunningTime();
            Time.timeScale = _currentTime;
        }

        private sealed class PauseScope : IDisposable
        {
            private TimeManager _manager;

            public PauseScope(TimeManager manager) => _manager = manager;

            public void Dispose()
            {
                if (_manager != null)
                    _manager.ReleasePause();

                _manager = null;
            }
        }
        
        public void SetBeforeTime(float time) => _beforeTime = time;

        private void Update()
        {
            Time.timeScale = _currentTime;
        }
    }
}
