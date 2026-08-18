using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.Agents.FSM;
using Member.KYM.Scripts.CoreSystems;
using UnityEngine;

namespace Member.KYM.Scripts.Players
{
    public class PlayerController : Agent
    {
        [field:Header("임시")]
        [field:SerializeField] public float JumpPower { get; private set; }
        [field:SerializeField] public int MaxJumpCount { get; private set; }
        
        [field:SerializeField] public PlayerInputSO PlayerInput { get; private set; }
        [SerializeField] private StateListSO stateList;
        private StateMachine _stateMachine;
        private int _currentJumpCount;

        protected override void InitializeModules()
        {
            base.InitializeModules();
            _stateMachine = new StateMachine(this, stateList.states);
;        }

        protected override void AfterInitializeModules()
        {
            base.AfterInitializeModules();
            PlayerInput.OnJumpKeyPressed += HandleJumpKeyPressed;
        }

        private void Start()
        {
            ChangeState(PlayerStateEnum.IDLE);
        }
        
        private void HandleJumpKeyPressed()
        {
            if (_currentJumpCount < MaxJumpCount)
            {
                ChangeState(PlayerStateEnum.JUMP);
                _currentJumpCount++;
            }
        }
        
        public void ResetJumpCount() => _currentJumpCount = 0;

        private void Update()
        {
            _stateMachine.UpdateMachine();
        }

        public void ChangeState(PlayerStateEnum state) => _stateMachine.ChangeState((int) state);
    }
}