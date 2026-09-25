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
        [SerializeField] private float sideDistance = 2.6f;
        [SerializeField] private float gatherDistance = 2.2f;
        [SerializeField] private float gatherDuration = 0.3f;
        [SerializeField] private float thrustDuration = 0.14f;
        [SerializeField] private float recoverDuration = 0.28f;
        [SerializeField] private Vector2 thrustHitbox = new Vector2(4.2f, 2.1f);
        [SerializeField] private float thrustDamage = 42f;

        private DamageCaster thrustCaster;

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
                Vector2 direction = (Boss.Target.position - Boss.transform.position).normalized;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                for (int i = 0; i < thrustSwords.Count; i++)
                {
                    Vector2 perpendicular = new Vector2(-direction.y, direction.x);
                    Vector3 gatherPoint = Boss.transform.position - (Vector3)direction * gatherDistance +
                                          (Vector3)perpendicular * (i - 1) * 0.55f + Vector3.up * 1.5f;
                    thrustSwords[i].MoveTo(gatherPoint, angle, gatherDuration / DurationScale);
                }
                Boss.AttackReady(Boss.Target.position);
                yield return new WaitForSeconds(gatherDuration / DurationScale);

                Vector3 targetPoint = Boss.Target.position;
                foreach (EnchantedSword sword in thrustSwords)
                    sword.MoveTo(targetPoint + (Vector3)direction * 1.4f, angle, thrustDuration / DurationScale, Ease.InExpo);

                thrustCaster.SetWorldPose(targetPoint, angle);
                thrustCaster.EnableCasting(
                    new DamageData(thrustDamage, DamageType.Melee, knockbackForce: direction * 5f),
                    thrustDuration / DurationScale
                );
                yield return new WaitForSeconds(thrustDuration / DurationScale);
                thrustCaster.DisableCasting();
                Boss.AttackImpact(targetPoint);
                Boss.ReturnControlledSwords();
                yield return new WaitForSeconds(recoverDuration / DurationScale);
            }
        }

        protected override void OnSwordmasterCancel()
        {
            thrustCaster?.DisableCasting();
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
