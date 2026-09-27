using System.Collections;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public class VolcanusPunchAttack : VolcanusSkill
    {
        [SerializeField] private Vector2 direction = Vector2.right;
        [SerializeField] private float readyDuration = 0.65f;
        [SerializeField] private float punchDuration = 0.24f;
        [SerializeField] private float punchHeight = 2.2f;
        [SerializeField] private Vector2 hitboxSize = new Vector2(3f, 2.6f);
        [SerializeField] private float damage = 1f;

        private Tween punchTween;

        protected override string DefaultActionState => Volcanus.ComboState;

        public override bool CanUseSkill(GameObject target = null) =>
            Boss != null && Boss.Target != null && !Boss.IsDead;

        protected override IEnumerator ExecuteVolcanus(GameObject target)
        {
            float side = Mathf.Approximately(direction.x, 0f) ? 1f : Mathf.Sign(direction.x);
            float groundY = Boss.GetGroundPoint(Boss.Target.position.x).y;
            float startX = Boss.ArenaCenter.x - Boss.ArenaHalfWidth * side;
            float endX = Boss.ArenaCenter.x + Boss.ArenaHalfWidth * side;
            Vector3 start = new Vector3(startX, groundY + punchHeight * Scale, Boss.transform.position.z);
            Vector3 end = new Vector3(endX, start.y, start.z);

            Boss.SetFacing(side);
            Boss.AttackReady(start);
            Boss.PlayFeedback(VolcanusFeedbackType.Ready, start);
            Boss.PlayAction(Volcanus.IdleState, ActionSpeed);
            Boss.AnimatePunch(start, end, side,
                readyDuration / ActionSpeed, punchDuration / ActionSpeed);
            yield return new WaitForSeconds(readyDuration / ActionSpeed);

            DamageCaster.ConfigureBox(hitboxSize * Scale, Boss.PlayerLayer);
            DamageCaster.SetWorldPose(start, 0f);
            DamageCaster.EnableCasting(new DamageData(damage, DamageType.Melee),
                punchDuration / ActionSpeed + Time.fixedDeltaTime);

            float progress = 0f;
            punchTween = DOTween.To(() => progress, value =>
                {
                    progress = value;
                    Vector3 point = Vector3.Lerp(start, end, value);
                    DamageCaster.SetWorldPose(point, 0f);
                }, 1f, punchDuration / ActionSpeed)
                .SetEase(Ease.InQuart)
                .SetTarget(this);
            yield return punchTween.WaitForCompletion();

            DamageCaster.DisableCasting();
            Boss.AttackImpact(end);
            Boss.PlayFeedback(VolcanusFeedbackType.Impact, end);
            Boss.Shake(false);
        }

        protected override void OnVolcanusCancel()
        {
            punchTween?.Kill();
        }
    }
}
