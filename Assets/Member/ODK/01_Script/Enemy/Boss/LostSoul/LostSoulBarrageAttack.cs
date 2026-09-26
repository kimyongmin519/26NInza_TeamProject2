using System.Collections;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    public class LostSoulBarrageAttack : LostSoulSkill
    {
        [SerializeField] private float castDistance = 8.5f;
        [SerializeField] private float castDelay = 1f;
        [SerializeField] private int bulletCount = 14;
        [SerializeField] private float bulletSpeed = 12f;
        [SerializeField] private float shotInterval = 0.08f;
        [SerializeField] private float randomAngle = 32f;
        [SerializeField] private int attackAnimationInterval = 4;

        protected override IEnumerator ExecuteLostSoul(GameObject target)
        {
            Boss.TeleportToTarget(castDistance);
            Boss.PlayAnimation("cast", 0.03f);
            Boss.PlayCastFeedback();
            yield return new WaitForSeconds(castDelay * DurationScale);
            for (int i = 0; i < bulletCount; i++)
            {
                if (i % Mathf.Max(1, attackAnimationInterval) == 0)
                    Boss.PlayAnimation("attack", 0f, 0.34f);
                Vector2 aimed = ((Vector2)target.transform.position - (Vector2)Boss.transform.position).normalized;
                Vector2 direction = Quaternion.Euler(0f, 0f, Random.Range(-randomAngle, randomAngle)) * aimed;
                Boss.SpawnSoul(Boss.transform.position, direction * Random.Range(bulletSpeed * 0.82f, bulletSpeed * 1.18f));
                if (i % 4 == 1)
                    Boss.SpawnWeakSoul(Boss.transform.position, direction * bulletSpeed * 0.5f);
                yield return new WaitForSeconds(shotInterval * DurationScale);
            }
        }
    }
}
