using System.Collections;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public class VolcanusSideSlashAttack : VolcanusSkill
    {
        [SerializeField] private float sideRate = 0.65f;
        [SerializeField] private float moveDuration = 0.65f;
        [SerializeField] private float slashReadyDuration = 0.45f;
        [SerializeField] private float slashDuration = 0.36f;
        [SerializeField] private float slashHeight = 2.2f;
        [SerializeField] private float slashThickness = 2.2f;
        [SerializeField] private float slamReadyDuration = 0.55f;
        [SerializeField] private float slamRadius = 2f;
        [SerializeField] private float damage = 1f;

        protected override string DefaultActionState => Volcanus.ComboState;

        public override bool CanUseSkill(GameObject target = null) =>
            Boss != null && Boss.Target != null && !Boss.IsDead;

        protected override IEnumerator ExecuteVolcanus(GameObject target)
        {
            float originX = Boss.transform.position.x;
            float side = Random.value < 0.5f ? -1f : 1f;
            float sideX = Boss.ArenaCenter.x + Boss.ArenaHalfWidth * side * sideRate;
            Tween move = Boss.MoveTo(sideX, Mathf.Abs(sideX - originX) / (moveDuration / ActionSpeed), true);
            if (move != null) yield return move.WaitForCompletion();

            float groundY = Boss.GetGroundPoint(Boss.ArenaCenter.x).y;
            Vector3 slashCenter = new Vector3(Boss.ArenaCenter.x, groundY + slashHeight * Scale,
                Boss.transform.position.z);
            Boss.SetFacing(-side);
            Boss.AttackReady(slashCenter);
            Boss.PlayFeedback(VolcanusFeedbackType.Ready, slashCenter);
            Vector3 slashStart = new Vector3(
                Boss.ArenaCenter.x + side * Boss.ArenaHalfWidth,
                slashCenter.y,
                Boss.transform.position.z);
            Vector3 slashEnd = new Vector3(
                Boss.ArenaCenter.x - side * Boss.ArenaHalfWidth,
                slashCenter.y,
                Boss.transform.position.z);
            Boss.AnimateSideSlash(slashStart, slashEnd, side,
                slashReadyDuration / ActionSpeed, slashDuration / ActionSpeed);
            yield return new WaitForSeconds(slashReadyDuration / ActionSpeed);

            Boss.PlayAction(Volcanus.IdleState, ActionSpeed);
            yield return new WaitForSeconds(slashDuration * 0.55f / ActionSpeed);
            DamageCaster.ConfigureBox(
                new Vector2(Boss.ArenaHalfWidth * 2f, slashThickness * Scale),
                Boss.PlayerLayer);
            DamageCaster.SetWorldPose(slashCenter, 0f);
            DamageCaster.Cast(new DamageData(damage, DamageType.Melee));
            Boss.AttackImpact(slashCenter);
            Boss.Shake(false);
            yield return new WaitForSeconds(slashDuration * 0.45f / ActionSpeed);

            Vector3 slamPoint = Boss.GetGroundPoint(Boss.Target.position.x);
            Vector3 fistHitPosition = Boss.GetFistVisualPosition(slamPoint);
            Vector3 fistReadyPosition = fistHitPosition + Vector3.up * (5f * Scale);
            Boss.AttackReady(slamPoint);
            Boss.PlayAction(Volcanus.IdleState, ActionSpeed);
            Boss.AnimateFistSlam(fistReadyPosition, fistHitPosition,
                slamReadyDuration / ActionSpeed, 0.16f / ActionSpeed);
            yield return new WaitForSeconds(slamReadyDuration / ActionSpeed);
            DamageCaster.ConfigureCircle(slamRadius * Scale, Boss.PlayerLayer);
            DamageCaster.SetWorldPosition(slamPoint);
            DamageCaster.Cast(new DamageData(damage, DamageType.Melee));
            Boss.SpawnFistImpactRocks(slamPoint);
            Boss.AttackImpact(slamPoint);
            Boss.PlayFeedback(VolcanusFeedbackType.Impact, slamPoint);
            Boss.Shake(true);

            yield return WalkTo(originX, false);
        }
    }
}
