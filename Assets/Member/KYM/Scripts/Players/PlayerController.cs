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

        [field: Header("숙이기")]
        [field: SerializeField, Range(0.1f, 1f)]
        public float CrouchHeightMultiplier { get; private set; } = 0.5f;
        
        [field:SerializeField] public PlayerInputSO PlayerInput { get; private set; }
        [SerializeField] private StateListSO stateList;
        public AgentSensor Sensor { get; private set; }
        public ISkillModule SkillModule { get; private set; }
        private StateMachine _stateMachine;
        private RobotArmGrappler _robotArmGrappler;
        private int _currentJumpCount;
        private CapsuleCollider2D _bodyCollider;
        private Vector2 _standingColliderSize;
        private Vector2 _standingColliderOffset;

        protected override void InitializeModules()
        {
            base.InitializeModules();
            _bodyCollider = GetComponent<CapsuleCollider2D>();
            Debug.Assert(_bodyCollider != null, $"{name}에 CapsuleCollider2D가 없습니다.");

            if (_bodyCollider != null)
            {
                _standingColliderSize = _bodyCollider.size;
                _standingColliderOffset = _bodyCollider.offset;
            }

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

            IMover mover = GetModule<IMover>();
            if (PlayerInput.MoveDirY < -0.5f)
            {
                if (mover.TryDropThroughPlatform())
                    ChangeState(PlayerStateEnum.FALL);

                return;
            }

            if (_currentJumpCount < MaxJumpCount)
            {
                ChangeState(PlayerStateEnum.JUMP);
                _currentJumpCount++;
            }
        }
        
        public void ResetJumpCount() => _currentJumpCount = 0;

        public void SetCrouching(bool isCrouching)
        {
            if (_bodyCollider == null)
                return;

            if (!isCrouching)
            {
                _bodyCollider.size = _standingColliderSize;
                _bodyCollider.offset = _standingColliderOffset;
                return;
            }

            float crouchHeight = _standingColliderSize.y * CrouchHeightMultiplier;
            float heightDifference = _standingColliderSize.y - crouchHeight;

            _bodyCollider.size = new Vector2(_standingColliderSize.x, crouchHeight);
            _bodyCollider.offset = new Vector2(
                _standingColliderOffset.x,
                _standingColliderOffset.y - heightDifference * 0.5f
            );
        }

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
