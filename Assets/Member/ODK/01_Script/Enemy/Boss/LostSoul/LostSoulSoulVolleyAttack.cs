using System.Collections;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    public class LostSoulSoulVolleyAttack : LostSoulSkill
    {
        [SerializeField] private float sideDistance = 5.2f;
        [SerializeField] private float airHeight = 2.8f;
        [SerializeField] private int waveCount = 3;
        [SerializeField] private int bulletsPerWave = 4;
        [SerializeField] private float bulletSpeed = 15f;
        [SerializeField] private float waveInterval = 0.18f;
        [SerializeField] private float laneSpacing = 0.46f;

        protected override IEnumerator ExecuteLostSoul(GameObject target)
        {
            Boss.TeleportToTarget(sideDistance, airHeight);
            yield return new WaitForSeconds(0.1f * DurationScale);

            for (int wave = 0; wave < waveCount; wave++)
            {
                ReplayAnimation();
                Vector2 direction = ((Vector2)target.transform.position - (Vector2)Boss.transform.position).normalized;
                Vector2 normal = new Vector2(-direction.y, direction.x);
                for (int i = 0; i < bulletsPerWave; i++)
                {
                    float offset = (i - (bulletsPerWave - 1) * 0.5f) * laneSpacing;
                    Boss.SpawnSoul(Boss.transform.position + (Vector3)(normal * offset), direction * bulletSpeed);
                }

                if (wave == 1)
                {
                    Vector2 weakDirection = Quaternion.Euler(0f, 0f, Random.Range(-7f, 7f)) * direction;
                    Boss.SpawnWeakSoul(Boss.transform.position, weakDirection * (bulletSpeed * 0.58f));
                }
                yield return new WaitForSeconds(waveInterval * DurationScale);
            }
        }
    }
}
