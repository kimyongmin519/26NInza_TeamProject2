using System.Collections;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    public class LostSoulTeleportSlashAttack : LostSoulSkill
    {
        [SerializeField] private float appearDelay = 0.08f;
        [SerializeField] private float activeDuration = 0.12f;
        [SerializeField] private Vector2 hitboxSize = new Vector2(3.8f, 2.5f);
        [SerializeField] private float hitboxForwardOffset = 1.2f;
        [SerializeField] private float damage = DamageCaster.BossPlayerDamage;
        [SerializeField] private bool waitForFullWindup = true;

        protected override IEnumerator ExecuteLostSoul(GameObject target)
        {
            Boss.TeleportToTarget();
            float impactDelay = appearDelay / DurationScale;
            if (waitForFullWindup)
                impactDelay = Mathf.Max(impactDelay, Boss.GetImpactDelay("teleport attack"));
            Boss.PlaySwing("teleport attack", impactDelay);

            float direction = target.transform.position.x >= Boss.transform.position.x ? 1f : -1f;
            Vector3 center = Boss.transform.position + new Vector3(direction * hitboxForwardOffset, 0.8f);
            Boss.AttackReady(center);
            yield return new WaitForSeconds(impactDelay);

            Caster.ConfigureBox(hitboxSize, Boss.PlayerLayer);
            Caster.SetWorldPose(center, 0f);
            Boss.PlaySlashFeedback();
            Caster.EnableCasting(new DamageData(damage, DamageType.Melee), activeDuration / DurationScale);
            Boss.AttackImpact(center);
            Boss.ShakeCamera(0.72f);
            yield return new WaitForSeconds(activeDuration / DurationScale);
        }
    }
}
