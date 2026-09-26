using DG.Tweening;
using KimLIb.AnimatorSystems;
using Member.KYM.Scripts.CombatSystems.SkillSystems;
using UnityEngine;

namespace Member.KYM.Scripts.Players.Skills
{
    public class PlayerDashSkill : AbstractPlayerSkill
    {
        [SerializeField] private AnimParamSO dashParam;
        [SerializeField] private float dashDistance = 4.5f;
        [SerializeField] private float dashDuration = 0.25f;
        [SerializeField, Min(0f)] private float wallGap = 0.05f;

        [Header("재사용")]
        [SerializeField, Min(0f)] private float dashRechargeDelay = 0.25f;

        [Header("대시 잔상 (직접 붙인 트레일 연결)")]
        [SerializeField] private TrailRenderer dashTrail;

        private bool _canDash = true;
        private float _dashRechargeReadyTime;
        private Collider2D _bodyCollider;
        private Vector2 _dashDirection;
        private float _dashTravelDistance;

        public float NormalizedRecharge
        {
            get
            {
                if (IsUsing || !_canDash)
                    return 0f;

                if (dashRechargeDelay <= 0f)
                    return 1f;

                float remainingTime = _dashRechargeReadyTime - Time.time;
                return Mathf.Clamp01(1f - remainingTime / dashRechargeDelay);
            }
        }

        private void Awake()
        {
            if (dashTrail != null)
            {
                dashTrail.emitting = false;
                dashTrail.Clear();
            }
        }

        public override void InitializeSkill(ISkillModule skillModule)
        {
            base.InitializeSkill(skillModule);
            _canDash = true;
            _dashRechargeReadyTime = 0f;
            _bodyCollider = _player.GetComponent<Collider2D>();
            Debug.Assert(_bodyCollider != null, "대시 이동을 검사할 플레이어 콜라이더가 없습니다.");
            _mover.OnGroundStatusChange += HandleGroundStatusChange;
        }

        public override bool CanUseSkill(GameObject target = null)
        {
            if (!_canDash || IsUsing || Time.time < _dashRechargeReadyTime || NormalizedCooldown < 1f)
                return false;

            float xInput = _player.PlayerInput.MoveDirX;
            float moveX = Mathf.Abs(xInput) > 0.05f ? Mathf.Sign(xInput) : _renderer.FacingDirection;
            _dashDirection = new Vector2(moveX, 0f);
            _dashTravelDistance = _player.Sensor.GetClearTravelDistance(
                _bodyCollider, _dashDirection, dashDistance, wallGap);
            return _dashTravelDistance > 0.01f;
        }

        public override void UseSkill(GameObject target = null)
        {
            if (!CanUseSkill(target))
                return;

            _canDash = false;
            base.UseSkill(target);
            _renderer.PlayClip(dashParam.ParamHash);

            _mover.CanManualMovement = false;
            _mover.SetGravityScale(0);
            _mover.StopImmediately(true, true);

            if (dashTrail != null)
            {
                dashTrail.Clear();
                dashTrail.emitting = true;
            }
            
            Vector3 destination = _player.transform.position + (Vector3)_dashDirection * _dashTravelDistance;
            float dashTime = _dashTravelDistance * dashDuration / dashDistance; //비례식을 이용하여 실제 이동거리만큼의 시간 구하기
            
            _player.transform.DOMove(destination, dashTime).SetEase(Ease.OutQuad).OnComplete(StopSkill);
        }

        public override void StopSkill()
        {
            if (dashTrail != null)
                dashTrail.emitting = false;

            base.StopSkill();
            _player.transform.DOKill(); //나갈때 강제로 이 트랜스폼에 걸린 모든 트윈을 제거하는거야.
            
            _mover.StopImmediately(true, false);
            _mover.CanManualMovement = true;
            _mover.SetGravityScale(1f);

            _dashRechargeReadyTime = Time.time + dashRechargeDelay;

            if (_mover.IsGrounded)
                _canDash = true;
        }

        private void HandleGroundStatusChange(bool isGrounded)
        {
            if (isGrounded)
                _canDash = true;
        }

        private void OnDestroy()
        {
            if (_mover != null)
                _mover.OnGroundStatusChange -= HandleGroundStatusChange;
        }

        private void OnDisable()
        {
            if (dashTrail != null)
            {
                dashTrail.emitting = false;
                dashTrail.Clear();
            }
        }

        private void OnValidate()
        {
            dashDistance = Mathf.Max(0.01f, dashDistance);
            dashDuration = Mathf.Max(0.01f, dashDuration);
            wallGap = Mathf.Max(0f, wallGap);
            dashRechargeDelay = Mathf.Max(0f, dashRechargeDelay);
        }
    }
}
