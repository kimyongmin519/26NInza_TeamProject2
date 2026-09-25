using System.Collections;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    public class LostSoulTeleportSlashAttack : LostSoulSkill
    {
        [SerializeField] private float appearDelay = 0.08f;
        [SerializeField] private float activeDuration = 0.12f;
        [SerializeField] private Vector2 hitboxSize = new Vector2(3.8f, 2.5f);
        [SerializeField] private float hitboxForwardOffset = 1.2f;
        [SerializeField] private float damage = 34f;

        protected override IEnumerator ExecuteLostSoul(GameObject target)
        {
            Boss.TeleportToTarget();
            yield return new WaitForSeconds(appearDelay * DurationScale);

            float direction = target.transform.position.x >= Boss.transform.position.x ? 1f : -1f;
            Vector3 center = Boss.transform.position + new Vector3(direction * hitboxForwardOffset, 0.8f);
            Caster.ConfigureBox(hitboxSize, Boss.PlayerLayer);
            Caster.SetWorldPose(center, 0f);
            Boss.AttackReady(center);
            Caster.EnableCasting(new DamageData(damage, DamageType.Melee), activeDuration * DurationScale);
            Boss.AttackImpact(center);
            yield return new WaitForSeconds(activeDuration * DurationScale);
        }
    }
}
