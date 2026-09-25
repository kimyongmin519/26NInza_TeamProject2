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
            float x = Mathf.Clamp(
                Boss.Target.position.x,
                Boss.ArenaCenter.x - Boss.ArenaHalfWidth + 1f,
                Boss.ArenaCenter.x + Boss.ArenaHalfWidth - 1f
            );
            Vector3 groundPoint = Boss.GetGroundPoint(x);
            groundPoint.z = Boss.transform.position.z;
            DrawTrajectory(Boss.transform.position, groundPoint, jumpHeight);
            Boss.AttackReady(groundPoint);
            Boss.PlayFeedback(VolcanusFeedbackType.Ready, groundPoint);
            Boss.PoseVisual(
                new Vector2(0f, -0.35f),
                0f,
                new Vector2(1.12f, 0.8f),
                warningDuration / DurationScale,
                Ease.InBack
            );
            yield return new WaitForSeconds(warningDuration / DurationScale);

            Boss.PoseVisual(
                new Vector2(0f, 0.3f),
                0f,
                new Vector2(0.86f, 1.18f),
                jumpDuration * 0.32f / DurationScale,
                Ease.OutExpo
            );
            yield return MoveArc(
                Boss.transform.position,
                groundPoint,
                jumpHeight * (Boss.IsPhaseTwo ? 1.25f : 1f),
                jumpDuration / DurationScale
            );
            if (trajectory != null) trajectory.enabled = false;

            DamageCaster.ConfigureCircle(landingRadius, Boss.PlayerLayer);
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
                    groundPoint + Vector3.up * 0.7f,
                    new Vector2(boulderLaunchVelocity.x * rate, boulderLaunchVelocity.y * Random.Range(0.85f, 1.15f))
                );
            }
            Boss.Shake(true);
            Boss.PlayFeedback(VolcanusFeedbackType.Impact, groundPoint);
            Boss.AttackImpact(groundPoint);
            Boss.ImpactVisual(Vector2.down, 0.5f, 0.24f / DurationScale);
            yield return new WaitForSeconds(0.25f / DurationScale);
            hasOrigin = false;
        }

        private IEnumerator MoveArc(Vector3 start, Vector3 end, float height, float duration)
        {
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
