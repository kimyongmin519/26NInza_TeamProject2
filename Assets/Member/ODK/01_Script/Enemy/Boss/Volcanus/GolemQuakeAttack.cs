using System.Collections;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public class GolemQuakeAttack : VolcanusSkill
    {
        [SerializeField] private float readyDuration = 0.65f;
        [SerializeField] private float impactRadius = 3.2f;
        [SerializeField] private float impactDamage = 48f;
        [SerializeField] private float waveSpeed = 11f;
        [SerializeField] private float waveDamage = 28f;
        [SerializeField] private float waveLifeTime = 2.2f;
        [SerializeField] private float phaseTwoWaveDelay = 0.22f;

        public override bool CanUseSkill(GameObject target = null) => Boss != null && !Boss.IsDead;

        protected override IEnumerator ExecuteVolcanus(GameObject target)
        {
            Vector3 groundPoint = Boss.GetGroundPoint(Boss.transform.position.x);
            Boss.AttackReady(groundPoint);
            Boss.PlayFeedback(VolcanusFeedbackType.Ready, groundPoint);
            Boss.PoseVisual(
                new Vector2(0f, -0.45f),
                0f,
                new Vector2(1.12f, 0.78f),
                readyDuration / DurationScale,
                Ease.InBack
            );
            yield return new WaitForSeconds(readyDuration / DurationScale);

            Boss.PoseVisual(
                new Vector2(0f, 0.22f),
                0f,
                new Vector2(0.88f, 1.14f),
                0.08f / DurationScale,
                Ease.OutExpo
            );
            Impact(groundPoint, 1f);
            if (Boss.IsPhaseTwo)
            {
                yield return new WaitForSeconds(phaseTwoWaveDelay / DurationScale);
                Impact(groundPoint, 0.75f);
            }
            Boss.ImpactVisual(Vector2.down, 0.42f, 0.22f / DurationScale);
            yield return new WaitForSeconds(0.3f / DurationScale);
        }

        private void Impact(Vector3 point, float damageScale)
        {
            DamageCaster.ConfigureCircle(impactRadius, Boss.PlayerLayer);
            DamageCaster.SetWorldPosition(point);
            DamageCaster.Cast(new DamageData(impactDamage * damageScale, DamageType.Melee));
            Vector3 wavePosition = point + Vector3.up * 0.4f;
            Boss.SpawnGroundWave(
                wavePosition,
                Vector2.left,
                waveSpeed,
                waveDamage * damageScale,
                waveLifeTime
            );
            Boss.SpawnGroundWave(
                wavePosition,
                Vector2.right,
                waveSpeed,
                waveDamage * damageScale,
                waveLifeTime
            );
            Boss.Shake(true);
            Boss.PlayFeedback(VolcanusFeedbackType.Quake, point);
            Boss.AttackImpact(point);
        }
    }
}
