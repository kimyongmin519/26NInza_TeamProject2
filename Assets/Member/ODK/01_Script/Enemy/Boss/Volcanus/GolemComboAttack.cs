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

        private Vector3 originPosition;
        private bool hasOrigin;

        public override bool CanUseSkill(GameObject target = null) =>
            Boss != null && Boss.Target != null && !Boss.IsDead;

        protected override IEnumerator ExecuteVolcanus(GameObject target)
        {
            originPosition = Boss.transform.position;
            hasOrigin = true;
            int strikeCount = Boss.IsPhaseTwo ? 3 : 2;
            Boss.PlayFeedback(VolcanusFeedbackType.Ready, Boss.transform.position);
            float firstDirection = Boss.Target.position.x >= Boss.transform.position.x ? 1f : -1f;
            Boss.PoseVisual(
                new Vector2(-firstDirection * 0.45f, -0.12f),
                firstDirection * 7f,
                new Vector2(0.94f, 1.06f),
                readyDuration / DurationScale,
                Ease.OutBack
            );
            yield return new WaitForSeconds(readyDuration / DurationScale);

            for (int i = 0; i < strikeCount && !Boss.IsDead; i++)
            {
                float direction = Boss.Target.position.x >= Boss.transform.position.x ? 1f : -1f;
                float desiredX = Boss.Target.position.x - direction * preferredDistance;
                desiredX = Mathf.Clamp(
                    desiredX,
                    Boss.ArenaCenter.x - Boss.ArenaHalfWidth + 1f,
                    Boss.ArenaCenter.x + Boss.ArenaHalfWidth - 1f
                );
                Boss.FaceTargetImmediately();
                ReplayAnimation();
                Boss.PoseVisual(
                    new Vector2(direction * 0.6f, 0.08f),
                    -direction * 9f,
                    new Vector2(1.1f, 0.92f),
                    stepDuration / DurationScale,
                    Ease.InCubic
                );
                Boss.transform.DOKill();
                yield return Boss.transform.DOMoveX(desiredX, stepDuration / DurationScale)
                    .SetEase(Ease.OutCubic)
                    .WaitForCompletion();

                Vector3 center = Boss.transform.position + new Vector3(
                    hitboxOffset.x * direction,
                    hitboxOffset.y,
                    0f
                );
                DamageCaster.ConfigureBox(hitboxSize, Boss.PlayerLayer);
                DamageCaster.SetWorldPose(center, 0f);
                DamageCaster.EnableCasting(
                    new DamageData(damage * (Boss.IsPhaseTwo ? 1.15f : 1f), DamageType.Melee),
                    strikeDuration / DurationScale
                );
                Boss.PlayFeedback(VolcanusFeedbackType.Impact, center);
                Boss.Shake(false);
                Boss.ImpactVisual(new Vector2(-direction, 0.18f), 0.32f, strikeDuration / DurationScale);
                yield return new WaitForSeconds(strikeDuration / DurationScale);
                DamageCaster.DisableCasting();
            }

            yield return Boss.transform.DOMove(originPosition, 0.35f / DurationScale)
                .SetEase(Ease.InOutSine)
                .WaitForCompletion();
            Boss.ResetVisual(0.2f / DurationScale);
            hasOrigin = false;
        }

        protected override void OnVolcanusCancel()
        {
            if (Boss == null) return;
            Boss.transform.DOKill();
            if (hasOrigin) Boss.transform.position = originPosition;
            hasOrigin = false;
        }
    }
}
