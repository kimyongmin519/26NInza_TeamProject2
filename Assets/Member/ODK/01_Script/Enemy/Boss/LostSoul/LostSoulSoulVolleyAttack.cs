using System.Collections;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    public class LostSoulSoulVolleyAttack : LostSoulSkill
    {
        [SerializeField] private float sideDistance = 5.2f;
        [SerializeField] private int waveCount = 3;
        [SerializeField] private int bulletsPerWave = 2;
        [SerializeField] private float bulletSpeed = 15f;
        [SerializeField] private float waveInterval = 0.18f;
        [SerializeField] private float laneSpacing = 0.46f;

        protected override IEnumerator ExecuteLostSoul(GameObject target)
        {
            Boss.TeleportToTarget(sideDistance);
            Boss.PlayCastFeedback();
            float release = Boss.PlayCastOnce();
            if (release > 0f) yield return new WaitForSeconds(release);

            for (int wave = 0; wave < waveCount; wave++)
            {
                float horizontalDirection = target.transform.position.x >= Boss.transform.position.x ? 1f : -1f;
                Vector3 ground = Boss.GetGroundPoint(target.transform.position.x);
                bool jumpLane = wave % 2 == 0;
                float laneY = ground.y + (jumpLane ? 0.62f : 1.72f);
                Vector3 laneOrigin = new Vector3(Boss.transform.position.x, laneY, Boss.transform.position.z);
                Vector2 direction = Vector2.right * horizontalDirection;
                for (int i = 0; i < bulletsPerWave; i++)
                {
                    float offset = (i - (bulletsPerWave - 1) * 0.5f) * laneSpacing * 0.18f;
                    Boss.SpawnSoul(laneOrigin + Vector3.up * offset, direction * bulletSpeed);
                }

                Vector3 weakOrigin = new Vector3(Boss.transform.position.x, ground.y + 1.12f, Boss.transform.position.z);
                Boss.SpawnWeakSoul(weakOrigin, direction * (bulletSpeed * 0.52f));
                yield return new WaitForSeconds(waveInterval / DurationScale);
            }
        }
    }
}
