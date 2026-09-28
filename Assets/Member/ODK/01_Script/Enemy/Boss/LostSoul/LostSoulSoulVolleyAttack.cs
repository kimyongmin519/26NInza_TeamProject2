using System.Collections;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    public class LostSoulSoulVolleyAttack : LostSoulSkill
    {
        [SerializeField] private float sideDistance = 5.2f;
        [SerializeField] private int waveCount = 2;
        [SerializeField] private int bulletsPerWave = 3;
        [SerializeField] private float bulletSpeed = 9f;
        [SerializeField] private float waveInterval = 0.55f;
        [SerializeField] private float fanAngle = 26f;
        [SerializeField] private float portalSideOffset = 1.6f;
        [SerializeField] private float portalHoldAfter = 0.2f;
        [SerializeField] private int weakSoulsPerWave = 2;

        private LostSoulPortal portal;

        protected override IEnumerator ExecuteLostSoul(GameObject target)
        {
            Boss.TeleportToTarget(sideDistance);
            Boss.PlayCastFeedback();
            float release = Boss.PlayCastOnce();
            float side = target.transform.position.x >= Boss.transform.position.x ? 1f : -1f;
            Vector3 portalPoint = Boss.GetPortalPoint(side * portalSideOffset);
            portal = Boss.OpenPortal(portalPoint);
            yield return new WaitForSeconds(Mathf.Max(release, Boss.PortalOpenDuration));

            for (int wave = 0; wave < waveCount; wave++)
            {
                if (target == null) break;
                Vector2 aimed = ((Vector2)target.transform.position - (Vector2)portalPoint).normalized;
                float offset = (wave % 2 == 0 ? -1f : 1f) * fanAngle * 0.25f;
                for (int i = 0; i < bulletsPerWave; i++)
                {
                    float rate = bulletsPerWave > 1 ? i / (float)(bulletsPerWave - 1) - 0.5f : 0f;
                    Vector2 direction = Quaternion.Euler(0f, 0f, rate * fanAngle * 2f + offset) * aimed;
                    Boss.SpawnSoul(portalPoint, direction * bulletSpeed);
                }
                for (int w = 0; w < weakSoulsPerWave; w++)
                    Boss.SpawnWeakSoul(portalPoint, (Vector2)(Quaternion.Euler(0f, 0f, (w - (weakSoulsPerWave - 1) * 0.5f) * 18f) * aimed) * (bulletSpeed * 0.52f));
                portal?.Pulse();
                yield return new WaitForSeconds(waveInterval / DurationScale);
            }

            yield return new WaitForSeconds(portalHoldAfter);
            ClosePortal();
        }

        private void ClosePortal()
        {
            if (portal != null) portal.Close(0.25f);
            portal = null;
        }

        protected override void OnLostSoulCompleted() => ClosePortal();
        protected override void OnLostSoulCancelled() => ClosePortal();
    }
}
