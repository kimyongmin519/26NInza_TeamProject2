using System;
using System.Collections;
using KimLIb.EventSystem;

namespace Member.KYM.Scripts.CoreSystems.Events
{
    // 화면을 가린 뒤 작업을 실행하고, 작업 완료 후 다시 화면을 연다.
    public sealed class TransitionRequest : GameEvent
    {
        public Func<IEnumerator> Operation { get; }
        public Action Finished { get; }
        public bool Accepted { get; set; }

        private TransitionRequest(Func<IEnumerator> operation, Action finished)
        {
            Operation = operation;
            Finished = finished;
        }

        public static bool TryRaise(EventChannelSO channel, Func<IEnumerator> operation, Action finished = null)
        {
            if (channel == null) return false;
            var request = new TransitionRequest(operation, finished);
            channel.RaiseEvent(request);
            return request.Accepted;
        }
    }
}
