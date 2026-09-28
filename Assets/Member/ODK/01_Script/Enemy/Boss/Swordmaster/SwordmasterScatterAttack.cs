using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Swordmaster
{
    public class SwordmasterScatterAttack : SwordmasterSkill
    {
        [SerializeField, Min(1)] private int swordCount = 8;
        [SerializeField] private float arenaPadding = 1.5f;
        [SerializeField] private float appearDuration = 0.25f;
        [SerializeField] private float appearStagger = 0.05f;
        [SerializeField] private float scatterSpeed = 9f;
        [SerializeField] private float scatterDuration = 0.45f;
        [SerializeField] private float aimWarning = 0.3f;
        [SerializeField] private float homeSpeed = 26f;
        [SerializeField] private float homeFlightTime = 1.6f;
        [SerializeField] private float retargetInterval = 0.12f;
        [SerializeField] private float pathLength = 14f;

        public override bool CanUseSkill(GameObject target = null) =>
            Boss != null && Boss.Target != null && !Boss.IsDead;

        protected override IEnumerator ExecuteSwordmaster(GameObject target)
        {
            Boss.AttackReady(Boss.Target.position);
            Boss.PlayAnimation(Swordmaster.JumpState);
            Boss.Cue(SwordmasterCue.CrossfireReady, Boss.transform.position);

            List<EnchantedSword> swords = Boss.TakeSwords(swordCount);
            float z = Boss.transform.position.z;
            for (int i = 0; i < swords.Count; i++)
            {
                Vector3 point = new Vector3(
                    Boss.ArenaCenter.x + Random.Range(-Boss.ArenaHalfWidth + arenaPadding, Boss.ArenaHalfWidth - arenaPadding),
                    Boss.ArenaCenter.y + Random.Range(-Boss.ArenaHalfHeight + arenaPadding, Boss.ArenaHalfHeight - arenaPadding),
                    z);
                swords[i].MoveTo(point, Random.Range(0f, 360f), appearDuration / DurationScale);
                yield return new WaitForSeconds(appearStagger / DurationScale);
            }
            yield return new WaitForSeconds(appearDuration / DurationScale);

            Boss.PlayAnimation(Swordmaster.Attack1State);
            Boss.Cue(SwordmasterCue.VolleyFire, Boss.transform.position);
            foreach (EnchantedSword sword in swords)
            {
                if (sword == null) continue;
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                sword.FireMagic(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), scatterSpeed, scatterDuration + aimWarning + retargetInterval * swords.Count + 1f, false);
            }
            yield return new WaitForSeconds(scatterDuration / DurationScale);

            for (int i = 0; i < swords.Count && !Boss.IsDead && Boss.Target != null; i++)
            {
                EnchantedSword sword = swords[i];
                if (sword == null || !sword.IsMagicFlying) continue;
                Vector3 from = sword.transform.position;
                Vector2 direction = (Boss.Target.position - from);
                if (direction.sqrMagnitude < 0.0001f) direction = Vector2.down;
                direction.Normalize();
                sword.Hover(direction);
                sword.ShowPathLine(from, from + (Vector3)(direction * pathLength), aimWarning / DurationScale);
                StartCoroutine(FireAfter(sword, direction, aimWarning / DurationScale));
                yield return new WaitForSeconds(retargetInterval / DurationScale);
            }
            yield return new WaitForSeconds(aimWarning / DurationScale);
            Boss.PlayAnimation(Swordmaster.Attack2State);
        }

        private IEnumerator FireAfter(EnchantedSword sword, Vector2 direction, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (sword == null || !sword.IsMagicFlying) yield break;
            Boss.Cue(SwordmasterCue.SwordLaunch, sword.transform.position);
            sword.FireMagic(direction, homeSpeed, homeFlightTime, false);
        }
    }
}
