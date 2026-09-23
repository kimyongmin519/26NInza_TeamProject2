using System.Collections.Generic;
using KimLIb.EventSystem;
using Member.KYM.Scripts.CoreSystems.Events;
using UnityEngine;

namespace Member.KYM.Scripts.CoreSystems.TimelineSystem
{
    public class TimelineManager : MonoBehaviour
    {
        [Header("이벤트")]
        [SerializeField] private EventChannelSO timelineChannel;

        private readonly Dictionary<int, TimelineTarget> _targets = new();
        private bool _isSubscribed;

        private void Awake()
        {
            RefreshTargets();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();

            foreach (TimelineTarget target in _targets.Values)
                target.CancelActivePlayback();
        }

        private void OnDestroy()
        {
            UnsubscribeTargets();
        }

        [ContextMenu("타임라인 대상 다시 찾기")]
        public void RefreshTargets()
        {
            UnsubscribeTargets();
            _targets.Clear();

            TimelineTarget[] targets =
                GetComponentsInChildren<TimelineTarget>(true);
            foreach (TimelineTarget target in targets)
            {
                if (!_targets.TryAdd(target.TimelineId, target))
                {
                    Debug.LogWarning(
                        $"TimelineId {target.TimelineId}가 중복되어 첫 번째 대상만 사용합니다.",
                        target);
                    continue;
                }

                target.PlaybackEnded += HandlePlaybackEnded;
            }
        }

        private void Subscribe()
        {
            if (_isSubscribed || timelineChannel == null)
                return;

            timelineChannel.AddListener<TimelinePlayRequestEvent>(HandlePlayRequest);
            timelineChannel.AddListener<TimelineStopRequestEvent>(HandleStopRequest);
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || timelineChannel == null)
                return;

            timelineChannel.RemoveListener<TimelinePlayRequestEvent>(HandlePlayRequest);
            timelineChannel.RemoveListener<TimelineStopRequestEvent>(HandleStopRequest);
            _isSubscribed = false;
        }

        private void UnsubscribeTargets()
        {
            foreach (TimelineTarget target in _targets.Values)
            {
                if (target != null)
                    target.PlaybackEnded -= HandlePlaybackEnded;
            }
        }

        private void HandlePlayRequest(TimelinePlayRequestEvent evt)
        {
            if (evt == null)
                return;

            if (_targets.TryGetValue(evt.TimelineId, out TimelineTarget target) &&
                target != null &&
                target.TryPlay(evt.RequestId))
            {
                return;
            }

            RaisePlaybackEnded(evt.TimelineId, evt.RequestId, false);
        }

        private void HandleStopRequest(TimelineStopRequestEvent evt)
        {
            if (evt == null ||
                !_targets.TryGetValue(evt.TimelineId, out TimelineTarget target) ||
                target == null)
            {
                return;
            }

            target.TryCancel(evt.RequestId);
        }

        private void HandlePlaybackEnded(
            TimelineTarget target,
            int requestId,
            bool completed)
        {
            RaisePlaybackEnded(target.TimelineId, requestId, completed);
        }

        private void RaisePlaybackEnded(
            int timelineId,
            int requestId,
            bool completed)
        {
            timelineChannel?.RaiseEvent(
                new TimelinePlaybackEndedEvent(
                    timelineId,
                    requestId,
                    completed));
        }
    }
}
