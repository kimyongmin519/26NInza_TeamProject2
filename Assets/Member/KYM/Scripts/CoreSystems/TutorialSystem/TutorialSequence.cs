using Member.KYM.Scripts.UI;
using UnityEngine;
using UnityEngine.Events;

namespace Member.KYM.Scripts.CoreSystems.TutorialSystem
{
    public class TutorialSequence : MonoBehaviour
    {
        [Header("진행 설정")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private TutorialPromptUI promptUI;
        [SerializeField] private UnityEvent onFinished;

        public bool IsRunning { get; private set; }
        public TutorialStep CurrentStep =>
            IsRunning && _currentIndex < _steps.Length ? _steps[_currentIndex] : null;

        private TutorialStep[] _steps;
        private int _currentIndex;

        private void Awake()
        {
            // 자식 TutorialStep의 하이어라키 순서가 진행 순서다.
            _steps = GetComponentsInChildren<TutorialStep>(true);
            promptUI?.Hide();
        }

        private void Start()
        {
            if (playOnStart)
                Begin();
        }

        private void OnDisable()
        {
            Stop();
        }

        public void Begin()
        {
            if (IsRunning)
                return;

            if (_steps.Length == 0)
            {
                Debug.LogWarning("TutorialSequence에 TutorialStep 자식이 없습니다.", this);
                return;
            }

            _currentIndex = 0;
            IsRunning = true;
            EnterCurrentStep();
        }

        public void Stop()
        {
            if (!IsRunning)
                return;

            CurrentStep?.Cancel();
            IsRunning = false;
            promptUI?.Hide();
        }

        internal void CompleteStep(TutorialStep step)
        {
            if (!IsRunning || CurrentStep != step)
                return;

            _currentIndex++;
            if (_currentIndex >= _steps.Length)
            {
                IsRunning = false;
                promptUI?.Hide();
                onFinished?.Invoke();
                return;
            }

            EnterCurrentStep();
        }

        private void EnterCurrentStep()
        {
            TutorialStep step = CurrentStep;
            if (step == null)
            {
                Debug.LogError("TutorialSequence에 비어 있는 단계가 있습니다.", this);
                Stop();
                return;
            }

            promptUI?.Show(step.Description);
            step.Enter(this);
        }
    }
}
