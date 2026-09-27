using System.Collections;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    public class LostSoulCrossCutAttack : LostSoulSkill
    {
        [SerializeField] private float warningDuration = 0.28f;
        [SerializeField] private float activeDuration = 0.16f;
        [SerializeField] private float cutThickness = 0.62f;
        [SerializeField] private float damage = 30f;
        [SerializeField] private float betweenCuts = 0.1f;
        [SerializeField] private Color beamColor = new Color(0.82f, 0.24f, 1f, 1f);

        protected override IEnumerator ExecuteLostSoul(GameObject target)
        {
            Boss.TeleportToTarget(3.8f);
            Vector3 center = target.transform.position;
            yield return TelegraphAndCut(center, 45f, -45f);
            center = target.transform.position;
            yield return TelegraphAndCut(center, 0f, 90f);
            yield return new WaitForSeconds(0.14f / DurationScale);
            center = target.transform.position;
            yield return TelegraphAndCut(center, 90f);
            yield return new WaitForSeconds(betweenCuts / DurationScale);
            yield return TelegraphAndCut(center, 0f);
        }

        private IEnumerator TelegraphAndCut(Vector3 center, params float[] angles)
        {
            Boss.AttackReady(center);
            float warning = warningDuration / DurationScale;
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
