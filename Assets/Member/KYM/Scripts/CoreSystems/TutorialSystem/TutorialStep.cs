using UnityEngine;
using UnityEngine.Events;

namespace Member.KYM.Scripts.CoreSystems.TutorialSystem
{
    public class TutorialStep : MonoBehaviour
    {
        [Header("안내")]
        [SerializeField, TextArea] private string description;

        [Header("단계 이벤트")]
        [SerializeField] private UnityEvent onStarted;
        [SerializeField] private UnityEvent onCompleted;

        public string Description => description;
        public bool IsActive { get; private set; }

        private TutorialSequence _sequence;

        internal void Enter(TutorialSequence sequence)
        {
            _sequence = sequence;
            IsActive = true;
            onStarted?.Invoke();
        }

        // 씬의 UnityEvent나 튜토리얼 전용 판정 컴포넌트에서 호출한다.
        public void Complete()
        {
            if (!IsActive)
                return;

            IsActive = false;
            onCompleted?.Invoke();
            _sequence?.CompleteStep(this);
            _sequence = null;
        }

        internal void Cancel()
        {
            IsActive = false;
            _sequence = null;
        }
    }
}
