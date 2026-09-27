using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public class GolemLeapSlamAttack : VolcanusSkill
    {
        [SerializeField] private float warningDuration = 0.45f;
        [SerializeField] private float jumpDuration = 0.68f;
        [SerializeField] private float jumpHeight = 3.2f;
        [SerializeField] private float landingRadius = 2.6f;
        [SerializeField] private float landingDamage = 58f;
        [SerializeField] private Vector2 boulderLaunchVelocity = new Vector2(3f, 8f);

        private LineRenderer trajectory;
        private Vector3 originPosition;
        private bool hasOrigin;

        protected override string DefaultActionState => Volcanus.SlamState;

        public override bool CanUseSkill(GameObject target = null) =>
            Boss != null && Boss.Target != null && !Boss.IsDead;

        protected override void OnVolcanusInitialize()
        {
            trajectory = Boss.SpawnTelegraphLine(transform);
            if (trajectory != null) trajectory.enabled = false;
        }

        protected override IEnumerator ExecuteVolcanus(GameObject target)
        {
            originPosition = Boss.transform.position;
            hasOrigin = true;
            float x = ClampToArena(Boss.Target.position.x);
            Vector3 groundPoint = Boss.GetGroundPoint(x);
            groundPoint.z = Boss.transform.position.z;
            float height = jumpHeight * Scale * (Boss.IsPhaseTwo ? 1.25f : 1f);
            DrawTrajectory(Boss.transform.position, groundPoint, height);
            Boss.AttackReady(groundPoint);
            Boss.PlayFeedback(VolcanusFeedbackType.Ready, groundPoint);

            Boss.PlayIdle();
            yield return new WaitForSeconds(warningDuration * 0.5f / ActionSpeed);

            float speed = ActionSpeed;
            float length = Boss.PlayAction(ActionState, speed);
            float impactTime = GetFirstImpactTime(ActionState);
            float airTime = Mathf.Min(jumpDuration / speed, impactTime);
            float windup = impactTime - airTime;
            if (windup > 0f) yield return new WaitForSeconds(windup);

            Boss.PlayFeedback(VolcanusFeedbackType.Step, Boss.transform.position);
            yield return MoveArc(Boss.transform.position, groundPoint, height, airTime);
            if (trajectory != null) trajectory.enabled = false;

            Land(groundPoint);
            hasOrigin = false;

            float recovery = length * 0.85f - impactTime;
            if (recovery > 0f) yield return new WaitForSeconds(recovery);
        }

        private void Land(Vector3 groundPoint)
        {
            DamageCaster.ConfigureCircle(landingRadius * Scale, Boss.PlayerLayer);
            DamageCaster.SetWorldPosition(groundPoint);
            DamageCaster.Cast(new DamageData(
                landingDamage * (Boss.IsPhaseTwo ? 1.2f : 1f),
                DamageType.Melee
            ));
            int boulderCount = Boss.IsPhaseTwo ? 4 : 2;
            for (int i = 0; i < boulderCount; i++)
            {
                float rate = boulderCount <= 1 ? 0f : Mathf.Lerp(-1f, 1f, i / (float)(boulderCount - 1));
                Boss.SpawnBoulder(
                    groundPoint + Vector3.up * 0.7f * Scale,
                    new Vector2(boulderLaunchVelocity.x * rate, boulderLaunchVelocity.y * Random.Range(0.85f, 1.15f))
                );
            }
            Boss.ImpactVisual(Vector2.down, 0.3f * Scale, 0.22f / ActionSpeed);
            Boss.Shake(true);
            Boss.PlayFeedback(VolcanusFeedbackType.Impact, groundPoint);
            Boss.AttackImpact(groundPoint);
        }

        private IEnumerator MoveArc(Vector3 start, Vector3 end, float height, float duration)
        {
            if (duration <= 0.01f)
            {
                Boss.transform.position = end;
                yield break;
            }
            float progress = 0f;
            Tween tween = DOTween.To(() => progress, value =>
                {
                    progress = value;
                    Vector3 point = Vector3.Lerp(start, end, value);
                    point.y += 4f * height * value * (1f - value);
                    Boss.transform.position = point;
                }, 1f, duration)
                .SetEase(Ease.Linear)
                .SetTarget(Boss.transform);
            yield return tween.WaitForCompletion();
        }

        private void DrawTrajectory(Vector3 start, Vector3 end, float height)
        {
            if (trajectory == null) return;
            const int count = 18;
            List<Vector3> points = new List<Vector3>(count);
            for (int i = 0; i < count; i++)
            {
                float rate = i / (float)(count - 1);
                Vector3 point = Vector3.Lerp(start, end, rate);
                point.y += 4f * height * rate * (1f - rate);
                points.Add(point);
            }
            trajectory.positionCount = points.Count;
            trajectory.SetPositions(points.ToArray());
            trajectory.enabled = true;
        }

        protected override void OnVolcanusCancel()
        {
            if (trajectory != null) trajectory.enabled = false;
            if (Boss == null) return;
            Boss.transform.DOKill();
            if (hasOrigin) Boss.transform.position = originPosition;
            hasOrigin = false;
        }
    }
}
