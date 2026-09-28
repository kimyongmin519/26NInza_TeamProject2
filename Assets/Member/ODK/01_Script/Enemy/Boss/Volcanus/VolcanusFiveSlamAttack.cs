using System.Collections;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public class VolcanusFiveSlamAttack : VolcanusSkill
    {
        [SerializeField] private int slamCount = 5;
        [SerializeField] private float slamWidthRate = 0.75f;
        [SerializeField] private float readyDuration = 0.42f;
        [SerializeField] private float slamDuration = 0.18f;
        [SerializeField] private float slamInterval = 0.28f;
        [SerializeField] private float impactRadius = 1.85f;
        [SerializeField] private float damage = 1f;

        protected override string DefaultActionState => Volcanus.SlamState;

        public override bool CanUseSkill(GameObject target = null) => Boss != null && !Boss.IsDead;

        protected override IEnumerator ExecuteVolcanus(GameObject target)
        {
            bool startFromRight = Random.value < 0.5f;
            float width = Boss.ArenaHalfWidth * slamWidthRate;
            float startX = Boss.ArenaCenter.x + (startFromRight ? width : -width);
            float endX = Boss.ArenaCenter.x - (startFromRight ? width : -width);

            for (int i = 0; i < Mathf.Max(1, slamCount) && !Boss.IsDead; i++)
            {
                float rate = slamCount <= 1 ? 0.5f : i / (float)(slamCount - 1);
                Vector3 point = Boss.GetGroundPoint(Mathf.Lerp(startX, endX, rate));
                Boss.AttackReady(point);
                Boss.PlayFeedback(VolcanusFeedbackType.Ready, point);

                float moveTime = i == 0 ? readyDuration : slamInterval;
                Tween move = Boss.MoveTo(point.x, Mathf.Abs(point.x - Boss.transform.position.x) /
                    (Mathf.Max(0.05f, moveTime) / ActionSpeed), true);
                if (move != null) yield return move.WaitForCompletion();

                Boss.PlayAction(Volcanus.IdleState, ActionSpeed);
                Vector3 fistHitPosition = Boss.GetFistVisualPosition(point);
                Vector3 fistReadyPosition = fistHitPosition + Vector3.up * (4.5f * Scale);
                Boss.AnimateFistSlam(fistReadyPosition, fistHitPosition,
                    readyDuration * 0.55f / ActionSpeed, slamDuration / ActionSpeed);
                yield return new WaitForSeconds(readyDuration * 0.55f / ActionSpeed);
                yield return new WaitForSeconds(slamDuration / ActionSpeed);

                DamageCaster.ConfigureCircle(impactRadius * Scale, Boss.PlayerLayer);
                DamageCaster.SetWorldPosition(point);
                DamageCaster.Cast(new DamageData(damage, DamageType.Melee));
                Boss.SpawnFistImpactRocks(point);
                Boss.AttackImpact(point);
                Boss.PlayFeedback(VolcanusFeedbackType.Impact, point);
                Boss.Shake(i == slamCount - 1);
                Boss.ResetVisual(0.1f / ActionSpeed);
            }
        }
    }
}
