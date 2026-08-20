using DG.Tweening;
using KimLIb.AnimatorSystems;
using Member.KYM.Scripts.Agents.FSM;
using UnityEngine;

namespace Member.KYM.Scripts.Players.Skills
{
    public class PlayerDashSkill : AbstractPlayerSkill
    {
        [SerializeField] private AnimParamSO dashParam;
        [SerializeField] private float dashDistance = 4.5f;
        [SerializeField] private float dashDuration = 0.25f;
        public override bool CanUseSkill(GameObject target = null)
        {
            return IsUsing == false && NormalizedCooldown >= 1f;
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target);
            
            _renderer.PlayClip(dashParam.ParamHash);
            
            float xInput = _player.PlayerInput.MoveDirX;
            float moveX = Mathf.Abs(xInput) > 0.05f ? Mathf.Sign(xInput) : _renderer.FacingDirection;
            Vector2 dashDirection = new Vector2(moveX, 0);

            _mover.CanManualMovement = false;
            _mover.SetGravityScale(0);
            _mover.StopImmediately(true, true);
            
            //Vector3 destination = _player.transform.position + (Vector3)dashDirection * _dashDistance;
            
            float realDistance = _player.Sensor.BoxCastObstacle(dashDirection, dashDistance, out RaycastHit2D hit);
            Vector3 destination = _player.transform.position + (Vector3)dashDirection * realDistance;
            float dashTime = realDistance * dashDuration / dashDistance; //비례식을 이용하여 실제 이동거리만큼의 시간 구하기
            
            _player.transform.DOMove(destination, dashTime).SetEase(Ease.OutQuad).OnComplete(StopSkill);
        }

        public override void StopSkill()
        {
            base.StopSkill();
            _player.transform.DOKill(); //나갈때 강제로 이 트랜스폼에 걸린 모든 트윈을 제거하는거야.
            
            _mover.StopImmediately(true, false);
            _mover.CanManualMovement = true;
            _mover.SetGravityScale(1f);
        }
    }
}