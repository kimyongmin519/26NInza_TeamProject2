using System;
using UnityEngine;
using UnityEngine.Playables;

namespace Member.KYM.Scripts.CoreSystems.TimelineSystem
{
    [DisallowMultipleComponent]
    public class TimelineTarget : MonoBehaviour
    {
        [Header("신호")]
        [field: SerializeField] public int TimelineId { get; private set; }

        [Header("타임라인")]
        [SerializeField] private PlayableDirector director;

        public event Action<TimelineTarget, int, bool> PlaybackEnded;

        private int _activeRequestId;
        private bool _isPlaying;
        private bool _isCancelling;

        private void Awake()
        {
            if (director == null)
                director = GetComponent<PlayableDirector>();
        }

        private void OnEnable()
        {
            if (director != null)
                director.stopped += HandleDirectorStopped;
        }

        private void OnDisable()
        {
            CancelActivePlayback();

            if (director != null)
                director.stopped -= HandleDirectorStopped;
        }

        public bool TryPlay(int requestId)
        {
            if (!isActiveAndEnabled || director == null || director.playableAsset == null)
                return false;

            if (_isPlaying)
                CancelActivePlayback();

            _activeRequestId = requestId;
            _isPlaying = true;
            _isCancelling = false;

            director.time = 0d;
            director.Play();
            return true;
        }

        public bool TryCancel(int requestId)
        {
            if (!_isPlaying || _activeRequestId != requestId)
                return false;

            CancelActivePlayback();
            return true;
        }

        public void CancelActivePlayback()
        {
            if (!_isPlaying)
                return;

            _isCancelling = true;
            director.Stop();

            // Director가 정지 이벤트를 보내지 않는 예외 상황도 정리한다.
            if (_isPlaying)
                FinishPlayback(false);
        }

        private void HandleDirectorStopped(PlayableDirector stoppedDirector)
        {
            if (!_isPlaying || stoppedDirector != director)
                return;

            FinishPlayback(!_isCancelling);
        }

        private void FinishPlayback(bool completed)
        {
            int requestId = _activeRequestId;

            _activeRequestId = 0;
            _isPlaying = false;
            _isCancelling = false;

            PlaybackEnded?.Invoke(this, requestId, completed);
        }

        private void OnValidate()
        {
            if (director == null)
                director = GetComponent<PlayableDirector>();
        }
    }
}
