using System.Collections;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public class VolcanusSawSlamAttack : VolcanusSkill
    {
        [SerializeField] private float readyDuration = 0.85f;
        [SerializeField] private float holdDuration = 0.45f;
        [SerializeField] private float slamDuration = 0.18f;
        [SerializeField] private float impactRadius = 2.2f;
        [SerializeField] private float damage = 1f;

        protected override string DefaultActionState => Volcanus.SlamState;

        public override bool CanUseSkill(GameObject target = null) =>
            Boss != null && Boss.Target != null && !Boss.IsDead;

        protected override IEnumerator ExecuteVolcanus(GameObject target)
        {
            Vector3 warningPoint = Boss.GetGroundPoint(Boss.Target.position.x);
            Boss.AttackReady(warningPoint);
            Boss.PlayFeedback(VolcanusFeedbackType.Ready, warningPoint);
            Vector3 readyPosition = Boss.GetSawVisualPosition(warningPoint) +
                Vector3.up * (5.5f * Scale);
            Boss.PrepareSawSlam(readyPosition, readyDuration / ActionSpeed);
            yield return new WaitForSeconds((readyDuration + holdDuration) / ActionSpeed);

            Vector3 hitPoint = Boss.GetGroundPoint(Boss.Target.position.x);
            Vector3 sawHitPosition = Boss.GetSawVisualPosition(hitPoint);
            Boss.AttackReady(hitPoint);
            Boss.PlayAction(Volcanus.IdleState, ActionSpeed);
            Boss.StrikeSaw(sawHitPosition, slamDuration / ActionSpeed);
            yield return new WaitForSeconds(slamDuration / ActionSpeed);

            DamageCaster.ConfigureCircle(impactRadius * Scale, Boss.PlayerLayer);
            DamageCaster.SetWorldPosition(hitPoint);
            DamageCaster.Cast(new DamageData(damage, DamageType.Melee));
            Boss.SpawnSawImpactRocks(hitPoint);
            Boss.AttackImpact(hitPoint);
            Boss.PlayFeedback(VolcanusFeedbackType.Quake, hitPoint);
            Boss.Shake(true);
        }
    }
}
