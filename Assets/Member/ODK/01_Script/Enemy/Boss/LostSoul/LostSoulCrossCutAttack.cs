using System.Collections;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    public class LostSoulCrossCutAttack : LostSoulSkill
    {
        [SerializeField] private LineRenderer[] warningLines;
        [SerializeField] private float warningDuration = 0.28f;
        [SerializeField] private float cutThickness = 0.62f;
        [SerializeField] private float damage = 30f;

        protected override IEnumerator ExecuteLostSoul(GameObject target)
        {
            Boss.TeleportToTarget(3.8f, 2.2f);
            Vector3 center = target.transform.position;
            yield return TelegraphAndCut(center, 45f, -45f);
            ReplayAnimation();
            center = target.transform.position;
            yield return TelegraphAndCut(center, 0f, 90f);
            yield return new WaitForSeconds(0.14f * DurationScale);
            ReplayAnimation();
            center = target.transform.position;
            yield return TelegraphAndCut(center, 90f, 0f);
        }

        private IEnumerator TelegraphAndCut(Vector3 center, float firstAngle, float secondAngle)
        {
            ShowLine(0, center, firstAngle);
            ShowLine(1, center, secondAngle);
            Boss.AttackReady(center);
            yield return new WaitForSeconds(warningDuration * DurationScale);
            HideLines();
            CastCut(center, firstAngle);
            CastCut(center, secondAngle);
            Boss.AttackImpact(center);
            yield return Boss.PulseOutline(0.08f * DurationScale);
        }

        private void ShowLine(int index, Vector3 center, float angle)
        {
            if (warningLines == null || index >= warningLines.Length) return;
            float length = Mathf.Max(Boss.ArenaHalfWidth * 2.4f, Boss.ArenaHalfHeight * 2.4f);
            Vector3 direction = Quaternion.Euler(0f, 0f, angle) * Vector3.right;
            LostSoul.SetLine(warningLines[index], center - direction * length, center + direction * length,
                new Color(0.85f, 0.28f, 1f, 0.72f), 0.08f);
        }

        private void CastCut(Vector3 center, float angle)
        {
            float length = Mathf.Max(Boss.ArenaHalfWidth * 2.8f, Boss.ArenaHalfHeight * 2.8f);
            Caster.ConfigureBox(new Vector2(length, cutThickness), Boss.PlayerLayer);
            Caster.SetWorldPose(center, angle);
            Caster.Cast(new DamageData(damage, DamageType.Beam));
        }

        private void HideLines()
        {
            if (warningLines == null) return;
            foreach (LineRenderer line in warningLines)
                if (line != null) line.enabled = false;
        }

        protected override void OnLostSoulCancelled() => HideLines();
    }
}
