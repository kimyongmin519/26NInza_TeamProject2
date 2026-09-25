using System.Collections;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    public class LostSoulHorizontalRiftAttack : LostSoulSkill
    {
        [SerializeField] private LineRenderer warningLine;
        [SerializeField] private float sideDistance = 5.5f;
        [SerializeField] private float airHeight = 2.4f;
        [SerializeField] private float warningDuration = 0.42f;
        [SerializeField] private float riftHeight = 0.65f;
        [SerializeField] private float damage = 42f;

        protected override IEnumerator ExecuteLostSoul(GameObject target)
        {
            Boss.TeleportToTarget(sideDistance, airHeight);
            float y = target.transform.position.y;
            Vector3 center = new Vector3(Boss.ArenaCenter.x, y, Boss.transform.position.z);
            Vector3 start = new Vector3(Boss.ArenaCenter.x - Boss.ArenaHalfWidth, y);
            Vector3 end = new Vector3(Boss.ArenaCenter.x + Boss.ArenaHalfWidth, y);
            LostSoul.SetLine(warningLine, start, end, new Color(0.75f, 0.25f, 1f, 0.72f), 0.09f);
            Boss.AttackReady(center);
            yield return new WaitForSeconds(warningDuration * DurationScale);
            if (warningLine != null) warningLine.enabled = false;

            Caster.ConfigureBox(new Vector2(Boss.ArenaHalfWidth * 2.15f, riftHeight), Boss.PlayerLayer);
            Caster.SetWorldPose(center, 0f);
            Caster.Cast(new DamageData(damage, DamageType.Beam));
            Boss.AttackImpact(center);
            yield return Boss.PulseOutline(0.11f * DurationScale);
        }

        protected override void OnLostSoulCancelled()
        {
            if (warningLine != null) warningLine.enabled = false;
        }
    }
}
