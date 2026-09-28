using System.Collections;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    public class LostSoulCrossCutAttack : LostSoulSkill
    {
        [SerializeField] private float warningDuration = 0.28f;
        [SerializeField] private float activeDuration = 0.16f;
        [SerializeField] private float cutThickness = 0.62f;
        [SerializeField] private float damage = DamageCaster.BossPlayerDamage;
        [SerializeField] private float betweenCuts = 0.1f;
        [SerializeField] private Color beamColor = new Color(0.94f, 0.9f, 1f, 1f);
        [SerializeField, Range(0.2f, 1f)] private float followUpWarningScale = 0.6f;
        [SerializeField] private float followUpGap = 0.04f;

        protected override IEnumerator ExecuteLostSoul(GameObject target)
        {
            Boss.TeleportToTarget(3.8f);
            yield return TelegraphAndCut(target.transform.position, warningDuration, 45f, -45f);
            yield return TelegraphAndCut(target.transform.position, warningDuration * followUpWarningScale, 0f, 90f);
            if (followUpGap > 0f) yield return new WaitForSeconds(followUpGap / DurationScale);
            yield return TelegraphAndCut(target.transform.position, warningDuration * followUpWarningScale, 45f, -45f);
            if (followUpGap > 0f) yield return new WaitForSeconds(followUpGap / DurationScale);
            yield return TelegraphAndCut(target.transform.position, warningDuration * followUpWarningScale, 0f, 90f);
        }

        private IEnumerator TelegraphAndCut(Vector3 center, float warningTime, params float[] angles)
        {
            Boss.AttackReady(center);
            float warning = warningTime / DurationScale;
            float active = activeDuration / DurationScale;
            float length = Mathf.Max(Boss.ArenaHalfWidth * 2.8f, Boss.ArenaHalfHeight * 2.8f);
            SwingAt(warning);
            foreach (float angle in angles)
            {
                Vector3 direction = Quaternion.Euler(0f, 0f, angle) * Vector3.right;
                Boss.SpawnSlashBeam(
                    center - direction * (length * 0.5f),
                    direction,
                    length,
                    cutThickness,
                    warning,
                    active,
                    damage,
                    beamColor
                );
            }
            yield return new WaitForSeconds(warning + active);
        }
    }
}
