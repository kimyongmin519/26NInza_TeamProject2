using KimLIb.EventSystem;
using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.Agents.FSM;
using Member.KYM.Scripts.CombatSystems.SkillSystems;
using Member.KYM.Scripts.CoreSystems;
using Member.KYM.Scripts.CoreSystems.Events;
using Member.KYM.Scripts.Players.FSM.Interface;
using Member.KYM.Scripts.Players.RobotArm;
using Member.ODK._01_Script;
using Member.ODK.Scripts;
using UnityEngine;
using UnityEngine.Events;

namespace Member.KYM.Scripts.Players
{
    public class PlayerController : Agent, IDamageable
    {
        [field:Header("임시")]
        [field:SerializeField] public float JumpPower { get; private set; }
        [field:SerializeField] public int MaxJumpCount { get; private set; }
        [field:SerializeField] public PlayerInputSO PlayerInput { get; private set; }
        [SerializeField] private StateListSO stateList;
        [field:SerializeField] public EventChannelSO UIChannel { get; private set; }
        
        [field:Header("PP")]
        [field:SerializeField] public EventChannelSO PostProcessChannel { get; private set; }

        [Header("피격 무적")]
        [SerializeField, Min(0f)] private float hitInvincibilityDuration = 1.5f;
        [SerializeField, Min(0.02f)] private float blinkInterval = 0.1f;

        [Header("투사체 잡기 표시 거리")]
        [SerializeField, Min(0f)] private float projectileCueRevealDistance = 2.8f;
        [SerializeField, Min(0f)] private float projectileCueFullRevealDistance = 1.2f;

        public float ProjectileCueRevealDistance => Mathf.Max(0f, projectileCueRevealDistance);
        public float ProjectileCueFullRevealDistance => Mathf.Clamp(
            projectileCueFullRevealDistance, 0f, ProjectileCueRevealDistance);
        
        public UnityEvent OnHit;
        public UnityEvent OnDeath;
        
        public AgentSensor Sensor { get; private set; }
        public ISkillModule SkillModule { get; private set; }
        private IMover _mover;
        private StateMachine _stateMachine;
        private RobotArmGrappler _robotArmGrappler;
        private int _currentJumpCount;
        private PlayerUIEventPublisher _uiPublisher;
        public int RemainingJumpCount => Mathf.Max(0, MaxJumpCount - _currentJumpCount);
        private CapsuleCollider2D _bodyCollider;
        private Vector2 _standingColliderSize;
        private Vector2 _standingColliderOffset;
        private SpriteRenderer[] _blinkRenderers;
        private bool[] _originalForceRenderingOff;
        private bool _isBlinking;
        private bool _blinkHidden;
        private float _nextBlinkTime;

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
            _mover = GetModule<IMover>();
            Sensor = GetModule<AgentSensor>();
            SkillModule = GetModule<ISkillModule>();
            _robotArmGrappler = GetComponentInChildren<RobotArmGrappler>(true);
        }

        protected override void AfterInitializeModules()
        {
            base.AfterInitializeModules();
            PlayerInput.OnJumpKeyPressed += HandleJumpKeyPressed;
            PlayerInput.OnDashKeyPressed += HandleDashKeyPressed;
            if (_mover != null)
                _mover.OnGroundStatusChange += HandleGroundStatusChange;

            if (HealthModule != null)
                HealthModule.OnDeath += HandleDeath;

            if (_robotArmGrappler != null)
            {
                _robotArmGrappler.GrappleStarted += HandleGrappleStarted;
                _robotArmGrappler.GrappleEnded += HandleGrappleEnded;
            }
        }

        private void HandleDashKeyPressed()
        {
            if (HealthModule != null && HealthModule.IsDead)
                return;

            if (_robotArmGrappler != null && _robotArmGrappler.IsGrappling)
                return;

            if (SkillModule.CanUseSkill(0))
            {
                ChangeState(PlayerStateEnum.SKILL);
                SkillModule.UseSkill(0);
            }
        }

        private void Start()
        {
            ChangeState(PlayerStateEnum.IDLE);
            
            _uiPublisher = new PlayerUIEventPublisher(this);
            _uiPublisher.Publish();
        }

        private void OnDestroy()
        {
            _uiPublisher?.Dispose();
            StopInvincibilityBlink();

            if (PlayerInput != null)
            {
                PlayerInput.OnJumpKeyPressed -= HandleJumpKeyPressed;
                PlayerInput.OnDashKeyPressed -= HandleDashKeyPressed;
            }

            if (HealthModule != null)
                HealthModule.OnDeath -= HandleDeath;

            if (_mover != null)
                _mover.OnGroundStatusChange -= HandleGroundStatusChange;

            if (_robotArmGrappler != null)
            {
                _robotArmGrappler.GrappleStarted -= HandleGrappleStarted;
                _robotArmGrappler.GrappleEnded -= HandleGrappleEnded;
            }
        }

        private void OnDisable()
        {
            StopInvincibilityBlink();
        }
        
        private void HandleJumpKeyPressed()
        {
            if (HealthModule != null && HealthModule.IsDead)
                return;

            if (_robotArmGrappler != null && _robotArmGrappler.IsGrappling)
                return;

            IMover mover = GetModule<IMover>();
            if (PlayerInput.MoveDirY < -0.5f && _stateMachine.CurrentState is ICanFallState)
            {
                if (mover.TryDropThroughPlatform())
                    ChangeState(PlayerStateEnum.FALL);

                return;
            }

            if (_currentJumpCount < MaxJumpCount && _stateMachine.CurrentState is ICanJumpState)
            {
                ChangeState(PlayerStateEnum.JUMP);
                _currentJumpCount++;
            }
        }
        
        public void ResetJumpCount() => _currentJumpCount = 0;

        private void HandleGroundStatusChange(bool isGrounded)
        {
            if (isGrounded)
                ResetJumpCount();
        }

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

            float crouchHeight = _standingColliderSize.y * 0.6f; //0.6 배율 만큼 콜라이더 사이즈 줄이기
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
            UpdateInvincibilityBlink();
        }

        private void LateUpdate()
        {
            _uiPublisher?.Publish();
        }

        private void HandleDeath()
        {
            StopInvincibilityBlink();
            StopCurrentAction();
            ChangeState(PlayerStateEnum.DEATH);
            _uiPublisher?.Publish();
            
            OnDeath?.Invoke();
        }

        private void StopCurrentAction()
        {
            ISkill currentSkill = SkillModule?.GetCurrentSkill();
            if (currentSkill is { IsUsing: true })
                currentSkill.StopSkill();

            if (_robotArmGrappler != null && _robotArmGrappler.IsGrappling)
                _robotArmGrappler.StopGrapple();
        }

        public void ChangeState(PlayerStateEnum state)
        {
            if (HealthModule != null &&
                HealthModule.IsDead &&
                state != PlayerStateEnum.DEATH)
            {
                return;
            }

            _stateMachine.ChangeState((int)state);
        }
        public void TakeDamage(DamageData damage)
        {
            if (HealthModule == null || HealthModule.IsDead || damage.Amount <= 0f)
                return;

            if (HealthModule.IsInvisible)
            {
                HealthModule.ApplyDamage(damage);
                return;
            }

            HealthModule.ApplyDamage(damage);

            if (!HealthModule.IsDead && hitInvincibilityDuration > 0f)
                HealthModule.SettingInvisibleTime(hitInvincibilityDuration);

            if (PostProcessChannel != null)
                PostProcessChannel.RaiseEvent(PostProcessEvents.HurtVignetteEvent.Play());

            if (HealthModule.IsDead)
                return;

            StopCurrentAction();
            ApplyKnockback(damage);
            ChangeState(PlayerStateEnum.HIT);
            
            OnHit?.Invoke();
        }

        private void UpdateInvincibilityBlink()
        {
            if (HealthModule == null || HealthModule.IsDead || !HealthModule.IsInvisible)
            {
                StopInvincibilityBlink();
                return;
            }

            if (!_isBlinking)
            {
                _blinkRenderers = GetComponentsInChildren<SpriteRenderer>(true);
                _originalForceRenderingOff = new bool[_blinkRenderers.Length];
                for (int i = 0; i < _blinkRenderers.Length; i++)
                    _originalForceRenderingOff[i] = _blinkRenderers[i].forceRenderingOff;

                _isBlinking = true;
                _nextBlinkTime = Time.time + blinkInterval;
            }

            if (Time.time < _nextBlinkTime)
                return;

            _blinkHidden = !_blinkHidden;
            for (int i = 0; i < _blinkRenderers.Length; i++)
            {
                if (_blinkRenderers[i] != null)
                    _blinkRenderers[i].forceRenderingOff =
                        _originalForceRenderingOff[i] || _blinkHidden;
            }

            _nextBlinkTime = Time.time + Mathf.Max(0.02f, blinkInterval);
        }

        private void StopInvincibilityBlink()
        {
            if (!_isBlinking)
                return;

            for (int i = 0; i < _blinkRenderers.Length; i++)
            {
                if (_blinkRenderers[i] != null)
                    _blinkRenderers[i].forceRenderingOff = _originalForceRenderingOff[i];
            }

            _blinkRenderers = null;
            _originalForceRenderingOff = null;
            _isBlinking = false;
            _blinkHidden = false;
        }
    }
}
