using System.Collections;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    public class LostSoulFourDirectionsAttack : LostSoulSkill
    {
        [SerializeField] private LineRenderer[] arrows;
        [SerializeField] private float arrowDuration = 0.72f;
        [SerializeField] private float responseTime = 0.48f;
        [SerializeField] private float moveDistance = 1.35f;
        [SerializeField] private float jumpDistance = 0.85f;
        [SerializeField] private float crouchHeightRatio = 0.82f;
        [SerializeField] private float damage = 36f;
        [SerializeField] private float damageRadius = 1.35f;

        protected override IEnumerator ExecuteLostSoul(GameObject target)
        {
            Boss.MoveToArenaCenter();
            DrawArrows();
            yield return new WaitForSeconds(arrowDuration * DurationScale);
            HideArrows();
            Boss.SetDarkness(true);

            int[] order = { 0, 1, 2, 3 };
            for (int i = order.Length - 1; i > 0; i--)
            {
                int swap = Random.Range(0, i + 1);
                (order[i], order[swap]) = (order[swap], order[i]);
            }

            Collider2D targetCollider = target.GetComponentInChildren<Collider2D>();
            for (int i = 0; i < order.Length; i++)
            {
                int cue = order[i];
                Vector3 before = target.transform.position;
                float beforeHeight = targetCollider != null ? targetCollider.bounds.size.y : 1f;
                yield return Boss.PulseOutline(0.09f);
                yield return new WaitForSeconds(responseTime * DurationScale);

                Vector3 after = target.transform.position;
                float afterHeight = targetCollider != null ? targetCollider.bounds.size.y : beforeHeight;
                bool avoided = cue switch
                {
                    0 => after.x <= before.x - moveDistance,
                    1 => after.x >= before.x + moveDistance,
                    2 => after.y >= before.y + jumpDistance,
                    _ => afterHeight <= beforeHeight * crouchHeightRatio || after.y <= before.y - 0.2f
                };

                if (!avoided)
                {
                    Caster.ConfigureCircle(damageRadius, Boss.PlayerLayer);
                    Caster.SetWorldPosition(after);
                    Caster.Cast(new DamageData(damage, DamageType.Special));
                }
            }

            Boss.SetDarkness(false);
            yield return new WaitForSeconds(0.16f * DurationScale);
        }

        private void DrawArrows()
        {
            if (arrows == null) return;
            Vector3 center = Boss.transform.position + Vector3.up * 3.7f;
            Vector3[] dirs = { Vector3.left, Vector3.right, Vector3.up, Vector3.down };
            for (int i = 0; i < arrows.Length && i < dirs.Length; i++)
            {
                LineRenderer line = arrows[i];
                if (line == null) continue;
                Vector3 dir = dirs[i];
                Vector3 normal = new Vector3(-dir.y, dir.x);
                Vector3 tip = center + dir * 1.15f;
                Vector3 basePoint = center + dir * 0.35f;
                line.useWorldSpace = true;
                line.positionCount = 4;
                line.SetPosition(0, basePoint + normal * 0.35f);
                line.SetPosition(1, tip);
                line.SetPosition(2, basePoint - normal * 0.35f);
                line.SetPosition(3, basePoint + normal * 0.35f);
                line.startColor = line.endColor = new Color(0.9f, 0.55f, 1f, 0.95f);
                line.widthMultiplier = 0.09f;
                line.enabled = true;
            }
        }

        private void HideArrows()
        {
            if (arrows == null) return;
            foreach (LineRenderer line in arrows)
                if (line != null) line.enabled = false;
        }

        protected override void OnLostSoulCancelled() => HideArrows();
    }
}
