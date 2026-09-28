using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Swordmaster
{
    public class SwordmasterTripleThrustAttack : SwordmasterSkill
    {
        [SerializeField] private int repeatCount = 3;
        [SerializeField] private float sideDistance = 4.4f;
        [SerializeField] private float gatherDistance = 3.6f;
        [SerializeField] private float gatherDuration = 0.3f;
        [SerializeField] private float thrustDuration = 0.14f;
        [SerializeField] private float recoverDuration = 0.28f;
        [SerializeField] private Vector2 thrustHitbox = new Vector2(5.6f, 2.1f);
        [SerializeField] private float thrustDamage = DamageCaster.BossPlayerDamage;
        [SerializeField] private float thrustOvershoot = 2.8f;
        [SerializeField] private float gatherSpread = 0.8f;

        [Header("Warning")]
        [SerializeField, Min(0.01f)] private float warningLineWidth = 0.12f;

        private DamageCaster thrustCaster;
        private SwordmasterTelegraph thrustTelegraph;

        public override bool CanUseSkill(GameObject target = null) =>
            Boss != null && Boss.Target != null && !Boss.IsDead;

        protected override void OnSwordmasterInitialize()
        {
            GameObject casterObject = new GameObject("Thrust Damage Caster");
            casterObject.transform.SetParent(transform, false);
            thrustCaster = casterObject.AddComponent<DamageCaster>();
            thrustCaster.ConfigureBox(thrustHitbox, Boss.PlayerLayer);
        }

        protected override IEnumerator ExecuteSwordmaster(GameObject target)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            Vector3 playerPosition = Boss.Target.position;
            Vector3 teleportPoint = Boss.GetGroundPoint(playerPosition.x + side * sideDistance);
            teleportPoint.z = Boss.transform.position.z;
            Boss.Teleport(teleportPoint);

            for (int repeat = 0; repeat < repeatCount && !Boss.IsDead; repeat++)
            {
                List<EnchantedSword> thrustSwords = Boss.TakeSwords(3);
                Vector3 targetPoint = Boss.Target.position;
                Vector2 direction = (targetPoint - Boss.transform.position).normalized;
                Vector3 thrustEnd = targetPoint + (Vector3)direction * thrustOvershoot;
                float pathDuration = (gatherDuration + thrustDuration) / DurationScale;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                for (int i = 0; i < thrustSwords.Count; i++)
                {
                    Vector2 perpendicular = new Vector2(-direction.y, direction.x);
                    Vector3 gatherPoint = Boss.transform.position - (Vector3)direction * gatherDistance +
                                          (Vector3)perpendicular * (i - 1) * gatherSpread + Vector3.up * 1.5f;
                    thrustSwords[i].MoveTo(gatherPoint, angle, gatherDuration / DurationScale);
                    thrustSwords[i].ShowPathLine(gatherPoint, thrustEnd, pathDuration);
                }
                Boss.AttackReady(targetPoint);
                Vector3 warningStart = Boss.GetHitCenter();
                thrustTelegraph = Boss.SpawnTelegraph();
                thrustTelegraph?.Show(
                    warningStart,
                    thrustEnd,
                    gatherDuration / DurationScale,
                    warningLineWidth
                );
                Boss.PlayAnimation(Swordmaster.JumpState);
                Boss.Cue(SwordmasterCue.ThrustGather, Boss.transform.position);
                yield return new WaitForSeconds(gatherDuration / DurationScale);
                Boss.ReleaseTelegraph(thrustTelegraph);
                thrustTelegraph = null;

                Boss.PlayAnimation(Swordmaster.Attack2State);
                foreach (EnchantedSword sword in thrustSwords)
                    sword.MoveTo(thrustEnd, angle, thrustDuration / DurationScale, Ease.InExpo);

                Vector3 hitStart = Boss.GetHitCenter();
                Vector3 hitCenter = Vector3.Lerp(hitStart, thrustEnd, 0.5f);
                float hitLength = Vector2.Distance(hitStart, thrustEnd);
                thrustCaster.ConfigureBox(new Vector2(hitLength, thrustHitbox.y), Boss.PlayerLayer);
                thrustCaster.SetWorldPose(hitCenter, angle);
                thrustCaster.EnableCasting(
                    new DamageData(thrustDamage, DamageType.Melee, knockbackForce: direction * 5f),
                    thrustDuration / DurationScale
                );
                yield return new WaitForSeconds(thrustDuration / DurationScale);
                thrustCaster.DisableCasting();
                thrustCaster.ClearWorldPose();
                Boss.AttackImpact(targetPoint);
                Boss.Cue(SwordmasterCue.ThrustStrike, targetPoint, angle);
                Boss.ReturnControlledSwords();
                yield return new WaitForSeconds(recoverDuration / DurationScale);
            }
        }

        protected override void OnSwordmasterCancel()
        {
            thrustCaster?.DisableCasting();
            thrustCaster?.ClearWorldPose();
            Boss?.ReleaseTelegraph(thrustTelegraph);
            thrustTelegraph = null;
        }

        private void OnDrawGizmosSelected()
        {
            Swordmaster boss = GetComponentInParent<Swordmaster>();
            if (boss == null) return;
            Gizmos.color = new Color(1f, 0.55f, 0.15f, 0.85f);
            Vector3 center = boss.transform.position + Vector3.right * sideDistance;
            Gizmos.DrawWireCube(center, thrustHitbox);
            Gizmos.DrawLine(boss.transform.position, center);
        }
    }
}
