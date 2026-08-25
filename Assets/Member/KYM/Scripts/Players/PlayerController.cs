using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.Agents.FSM;
using Member.KYM.Scripts.CombatSystems.SkillSystems;
using Member.KYM.Scripts.CoreSystems;
using Member.KYM.Scripts.Players.Equipment;
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
        public AgentSensor Sensor { get; private set; }
        public ISkillModule SkillModule { get; private set; }
        public PlayerEquipmentController EquipmentController { get; private set; }
        public bool IsEquipmentActionActive { get; private set; }
        private StateMachine _stateMachine;
        private int _currentJumpCount;

        protected override void InitializeModules()
        {
            base.InitializeModules();
            _stateMachine = new StateMachine(this, stateList.states);
            Sensor = GetModule<AgentSensor>();
            SkillModule = GetModule<ISkillModule>();
            EquipmentController = GetModule<PlayerEquipmentController>();
        }

        protected override void AfterInitializeModules()
        {
            base.AfterInitializeModules();
            PlayerInput.OnJumpKeyPressed += HandleJumpKeyPressed;
            PlayerInput.OnDashKeyPressed += HandleDashKeyPressed;
        }

        private void HandleDashKeyPressed()
        {
            if (IsEquipmentActionActive)
                return;

            if (SkillModule.CanUseSkill(0))
            {
                SkillModule.UseSkill(0);
            }
        }

        private void Start()
        {
            ChangeState(PlayerStateEnum.IDLE);
        }

        private void OnDestroy()
        {
            if (PlayerInput != null)
            {
                PlayerInput.OnJumpKeyPressed -= HandleJumpKeyPressed;
                PlayerInput.OnDashKeyPressed -= HandleDashKeyPressed;
            }
        }
        
        private void HandleJumpKeyPressed()
        {
            if (IsEquipmentActionActive)
                return;

            if (_currentJumpCount < MaxJumpCount)
            {
                ChangeState(PlayerStateEnum.JUMP);
                _currentJumpCount++;
            }
        }
        
        public void ResetJumpCount() => _currentJumpCount = 0;

        public void BeginEquipmentAction(PlayerStateEnum state)
        {
            IsEquipmentActionActive = true;
            ChangeState(state);
        }

        public void EndEquipmentAction()
        {
            if (!IsEquipmentActionActive)
                return;

            IsEquipmentActionActive = false;
            IMover mover = GetModule<IMover>();
            ChangeState(
                mover.IsGrounded
                    ? PlayerStateEnum.IDLE
                    : PlayerStateEnum.FALL
            );
        }

        private void Update()
        {
            _stateMachine.UpdateMachine();
        }

        public void ChangeState(PlayerStateEnum state) => _stateMachine.ChangeState((int) state);
    }
}
