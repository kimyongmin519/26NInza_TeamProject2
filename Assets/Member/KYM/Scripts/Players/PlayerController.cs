using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.Agents.FSM;
using Member.KYM.Scripts.CombatSystems.SkillSystems;
using Member.KYM.Scripts.CoreSystems;
using Member.KYM.Scripts.Players.RobotArm;
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
        private StateMachine _stateMachine;
        private RobotArmGrappler _robotArmGrappler;
        private int _currentJumpCount;

        protected override void InitializeModules()
        {
            base.InitializeModules();
            _stateMachine = new StateMachine(this, stateList.states);
            Sensor = GetModule<AgentSensor>();
            SkillModule = GetModule<ISkillModule>();
            _robotArmGrappler = GetComponentInChildren<RobotArmGrappler>(true);
        }

        protected override void AfterInitializeModules()
        {
            base.AfterInitializeModules();
            PlayerInput.OnJumpKeyPressed += HandleJumpKeyPressed;
            PlayerInput.OnDashKeyPressed += HandleDashKeyPressed;

            if (_robotArmGrappler != null)
            {
                _robotArmGrappler.GrappleStarted += HandleGrappleStarted;
                _robotArmGrappler.GrappleEnded += HandleGrappleEnded;
            }
        }

        private void HandleDashKeyPressed()
        {
            if (_robotArmGrappler != null && _robotArmGrappler.IsGrappling)
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

            if (_robotArmGrappler != null)
            {
                _robotArmGrappler.GrappleStarted -= HandleGrappleStarted;
                _robotArmGrappler.GrappleEnded -= HandleGrappleEnded;
            }
        }
        
        private void HandleJumpKeyPressed()
        {
            if (_robotArmGrappler != null && _robotArmGrappler.IsGrappling)
                return;

            if (_currentJumpCount < MaxJumpCount)
            {
                ChangeState(PlayerStateEnum.JUMP);
                _currentJumpCount++;
            }
        }
        
        public void ResetJumpCount() => _currentJumpCount = 0;

        private void HandleGrappleStarted()
        {
            ChangeState(PlayerStateEnum.GRAPPLE);
        }

        private void HandleGrappleEnded()
        {
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
