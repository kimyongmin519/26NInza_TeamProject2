using System.Collections;
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
        [SerializeField] private float slamForwardOffset = 1.4f;

        protected override string DefaultActionState => Volcanus.SlamState;

        public override bool CanUseSkill(GameObject target = null) => Boss != null && !Boss.IsDead;

        protected override IEnumerator ExecuteVolcanus(GameObject target)
        {
            float direction = Boss.FacingToTarget();
            Vector3 groundPoint = Boss.GetGroundPoint(Boss.transform.position.x + direction * slamForwardOffset * Scale);
            groundPoint.z = Boss.transform.position.z;
            Boss.AttackReady(groundPoint);
            Boss.PlayFeedback(VolcanusFeedbackType.Ready, groundPoint);

            yield return PerformAction(ActionState, (index, rate) =>
            {
                Impact(groundPoint, 1f);
                if (Boss.IsPhaseTwo) StartCoroutine(SecondImpact(groundPoint));
            });
        }

        private IEnumerator SecondImpact(Vector3 point)
        {
            yield return new WaitForSeconds(phaseTwoWaveDelay / ActionSpeed);
            if (Boss == null || Boss.IsDead) yield break;
            Impact(point, 0.75f);
        }

        private void Impact(Vector3 point, float damageScale)
        {
            DamageCaster.ConfigureCircle(impactRadius * Scale, Boss.PlayerLayer);
            DamageCaster.SetWorldPosition(point);
            DamageCaster.Cast(new DamageData(impactDamage * damageScale, DamageType.Melee));
            Vector3 wavePosition = point + Vector3.up * 0.4f * Scale;
            Boss.SpawnGroundWave(wavePosition, Vector2.left, waveSpeed, waveDamage * damageScale, waveLifeTime);
            Boss.SpawnGroundWave(wavePosition, Vector2.right, waveSpeed, waveDamage * damageScale, waveLifeTime);
            Boss.ImpactVisual(Vector2.down, 0.25f * Scale, 0.2f / ActionSpeed);
            Boss.Shake(true);
            Boss.PlayFeedback(VolcanusFeedbackType.Quake, point);
            Boss.AttackImpact(point);
        }
    }
}
