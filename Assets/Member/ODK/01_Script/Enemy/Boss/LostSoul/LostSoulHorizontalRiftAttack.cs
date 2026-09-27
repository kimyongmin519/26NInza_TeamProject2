using System.Collections;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    public class LostSoulHorizontalRiftAttack : LostSoulSkill
    {
        [SerializeField] private float sideDistance = 5.5f;
        [SerializeField] private float warningDuration = 0.42f;
        [SerializeField] private float activeDuration = 0.18f;
        [SerializeField] private float riftHeight = 0.65f;
        [SerializeField] private float damage = 42f;
        [SerializeField] private Color beamColor = new Color(0.78f, 0.2f, 1f, 1f);

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
        }
    }
}
