using System.Collections;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public class GolemComboAttack : VolcanusSkill
    {
        [SerializeField] private float readyDuration = 0.35f;
        [SerializeField] private float stepDuration = 0.18f;
        [SerializeField] private float strikeDuration = 0.16f;
        [SerializeField] private float preferredDistance = 2.2f;
        [SerializeField] private Vector2 hitboxSize = new Vector2(4f, 3.2f);
        [SerializeField] private Vector2 hitboxOffset = new Vector2(1.8f, 1.6f);
        [SerializeField] private float damage = 34f;
        [SerializeField] private float stepDistance = 0.55f;
        [SerializeField] private float runDistance = 7f;
        [SerializeField] private float finisherDamageMultiplier = 1.35f;

        protected override string DefaultActionState => Volcanus.ComboState;

        public override bool CanUseSkill(GameObject target = null) =>
            Boss != null && Boss.Target != null && !Boss.IsDead;

        protected override IEnumerator ExecuteVolcanus(GameObject target)
        {
            float direction = Boss.FacingToTarget();
            float desiredX = ClampToArena(Boss.Target.position.x - direction * preferredDistance * Scale);
            bool run = Mathf.Abs(desiredX - Boss.transform.position.x) > runDistance * Scale;
            yield return WalkTo(desiredX, run);

            direction = Boss.FacingToTarget();
            Boss.PlayFeedback(VolcanusFeedbackType.Ready, Boss.transform.position);
            Boss.AttackReady(Boss.transform.position + Vector3.right * direction * hitboxOffset.x * Scale);
            if (readyDuration > 0f)
            {
                Boss.PlayIdle();
                yield return new WaitForSeconds(readyDuration * 0.5f / ActionSpeed);
            }

            yield return PerformAction(ActionState, (index, rate) => Strike(index, rate));
        }

        private void Strike(int index, float rate)
        {
            float direction = Boss.FacingToTarget();
            Boss.SetFacing(direction);
            float nextX = ClampToArena(Boss.transform.position.x + direction * stepDistance * Scale);
            Boss.transform.DOKill();
            Boss.transform.DOMoveX(nextX, stepDuration / ActionSpeed).SetEase(Ease.OutCubic);

            bool finisher = rate >= 1f;
            Vector3 center = Boss.transform.position + new Vector3(
                hitboxOffset.x * Scale * direction,
                hitboxOffset.y * Scale,
                0f
            );
            float amount = damage * (Boss.IsPhaseTwo ? 1.15f : 1f) * (finisher ? finisherDamageMultiplier : 1f);
            DamageCaster.ConfigureBox(hitboxSize * Scale, Boss.PlayerLayer);
            DamageCaster.SetWorldPose(center, 0f);
            DamageCaster.EnableCasting(new DamageData(amount, DamageType.Melee), strikeDuration / ActionSpeed);
            Boss.PlayFeedback(VolcanusFeedbackType.Impact, center);
            Boss.Shake(finisher);
            Boss.AttackImpact(center);
        }
    }
}
