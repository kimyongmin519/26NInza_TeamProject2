using System.Collections;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    public class LostSoulBarrageAttack : LostSoulSkill
    {
        [SerializeField] private float castDistance = 8.5f;
        [SerializeField] private int bulletCount = 8;
        [SerializeField] private float bulletSpeed = 8.5f;
        [SerializeField] private float shotInterval = 0.2f;
        [SerializeField] private float randomAngle = 24f;
        [SerializeField] private int weakSoulEvery = 7;
        [SerializeField] private float portalHoldAfter = 0.2f;

        private LostSoulPortal portal;

        protected override IEnumerator ExecuteLostSoul(GameObject target)
        {
            Boss.TeleportToTarget(castDistance);
            Boss.PlayCastFeedback();
            float release = Boss.PlayCastOnce();
            Vector3 portalPoint = Boss.GetPortalPoint();
            portal = Boss.OpenPortal(portalPoint);
            yield return new WaitForSeconds(Mathf.Max(release, Boss.PortalOpenDuration));

            for (int i = 0; i < bulletCount; i++)
            {
                if (target == null) break;
                Vector2 aimed = ((Vector2)target.transform.position - (Vector2)portalPoint).normalized;
                Vector2 direction = Quaternion.Euler(0f, 0f, Random.Range(-randomAngle, randomAngle)) * aimed;
                Boss.SpawnSoul(portalPoint, direction * Random.Range(bulletSpeed * 0.88f, bulletSpeed * 1.12f));
                if (weakSoulEvery > 0 && i % weakSoulEvery == weakSoulEvery - 1)
                    Boss.SpawnWeakSoul(portalPoint, direction * bulletSpeed * 0.5f);
                portal?.Pulse();
                yield return new WaitForSeconds(shotInterval / DurationScale);
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
