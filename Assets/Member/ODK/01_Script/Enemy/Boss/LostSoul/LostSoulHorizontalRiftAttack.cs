using System.Collections;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    public class LostSoulHorizontalRiftAttack : LostSoulSkill
    {
        [Header("Rift")]
        [SerializeField] private float sideDistance = 5.5f;
        [SerializeField] private float warningDuration = 0.42f;
        [SerializeField] private float activeDuration = 0.18f;
        [SerializeField] private float riftHeight = 0.65f;
        [SerializeField] private float damage = DamageCaster.BossPlayerDamage;
        [SerializeField] private Color beamColor = new Color(0.94f, 0.9f, 1f, 1f);

        [Header("Soul Burst")]
        [SerializeField] private float castGap = 0.25f;
        [SerializeField] private Vector2Int burstCountRange = new Vector2Int(12, 17);
        [SerializeField] private float burstSpeed = 8f;
        [SerializeField] private int burstWeakSouls = 3;
        [SerializeField] private float weakSoulSpeedScale = 0.55f;
        [SerializeField] private float burstShake = 0.35f;
        [SerializeField] private float portalHoldAfter = 0.3f;

        private LostSoulPortal portal;

        protected override IEnumerator ExecuteLostSoul(GameObject target)
        {
            Boss.TeleportToTarget(sideDistance);
            float warning = warningDuration / DurationScale;
            float active = activeDuration / DurationScale;
            float y = target.transform.position.y;
            Vector3 start = new Vector3(Boss.ArenaCenter.x - Boss.ArenaHalfWidth, y, Boss.transform.position.z);
            float length = Boss.ArenaHalfWidth * 2f;
            Boss.AttackReady(start + Vector3.right * (length * 0.5f));
            SwingAt(warning);
            Boss.SpawnSlashBeam(
                start,
                Vector2.right,
                length,
                riftHeight,
                warning,
                active,
                damage,
                beamColor
            );
            yield return new WaitForSeconds(warning + active);
            if (target == null || Boss.IsDead) yield break;

            if (castGap > 0f) yield return new WaitForSeconds(castGap / DurationScale);
            Boss.PlayCastFeedback();
            float release = Boss.PlayCastOnce();
            Vector3 portalPoint = Boss.GetPortalPoint();
            portal = Boss.OpenPortal(portalPoint);
            yield return new WaitForSeconds(Mathf.Max(release, Boss.PortalOpenDuration));

            int minimum = Mathf.Max(1, Mathf.Min(burstCountRange.x, burstCountRange.y));
            int maximum = Mathf.Max(minimum, Mathf.Max(burstCountRange.x, burstCountRange.y));
            int count = Random.Range(minimum, maximum + 1);
            int weakCount = Mathf.Clamp(burstWeakSouls, 0, count);
            int weakStep = weakCount > 0 ? Mathf.Max(1, count / weakCount) : 0;
            int weakOffset = weakStep > 0 ? Random.Range(0, weakStep) : 0;
            float startAngle = Random.Range(0f, 360f);
            int weakSpawned = 0;

            for (int i = 0; i < count; i++)
            {
                float radians = (startAngle + 360f * i / count) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                bool weak = weakStep > 0 && weakSpawned < weakCount && (i - weakOffset) % weakStep == 0 && i >= weakOffset;
                if (weak)
                {
                    Boss.SpawnWeakSoul(portalPoint, direction * burstSpeed * weakSoulSpeedScale);
                    weakSpawned++;
                }
                else
                {
                    Boss.SpawnSoul(portalPoint, direction * burstSpeed);
                }
            }
            portal?.Pulse();
            Boss.ShakeCamera(burstShake);

            yield return new WaitForSeconds(portalHoldAfter);
            ClosePortal();
        }

        private void ClosePortal()
        {
            portal?.Close();
            portal = null;
        }

        protected override void OnLostSoulCompleted() => ClosePortal();
        protected override void OnLostSoulCancelled() => ClosePortal();
    }
}
