using System.Collections;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    public class LostSoulBarrageAttack : LostSoulSkill
    {
        [SerializeField] private float castDelay = 1f;
        [SerializeField] private int bulletCount = 28;
        [SerializeField] private float bulletSpeed = 12f;
        [SerializeField] private float shotInterval = 0.045f;
        [SerializeField] private float randomAngle = 32f;

        protected override IEnumerator ExecuteLostSoul(GameObject target)
        {
            yield return new WaitForSeconds(castDelay * DurationScale);
            for (int i = 0; i < bulletCount; i++)
            {
                Vector2 aimed = ((Vector2)target.transform.position - (Vector2)Boss.transform.position).normalized;
                Vector2 direction = Quaternion.Euler(0f, 0f, Random.Range(-randomAngle, randomAngle)) * aimed;
                Boss.SpawnSoul(Boss.transform.position, direction * Random.Range(bulletSpeed * 0.82f, bulletSpeed * 1.18f));
                if (i == bulletCount / 2)
                    Boss.SpawnWeakSoul(Boss.transform.position, direction * bulletSpeed * 0.5f);
                yield return new WaitForSeconds(shotInterval * DurationScale);
            }
        }
    }
}
