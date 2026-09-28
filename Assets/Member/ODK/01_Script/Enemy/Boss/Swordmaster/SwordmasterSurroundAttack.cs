using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Swordmaster
{
    public class SwordmasterSurroundAttack : SwordmasterSkill
    {
        [Header("Rounds")]
        [SerializeField, Min(1)] private int roundCount = 3;
        [SerializeField] private Vector2Int swordsPerRound = new Vector2Int(2, 4);
        [SerializeField] private float roundGap = 0.25f;

        [Header("Final")]
        [SerializeField, Min(1)] private int finalSwordCount = 6;
        [SerializeField] private float finalFireInterval = 0.16f;

        [Header("Placement")]
        [SerializeField] private Vector2 radiusRange = new Vector2(3.2f, 5f);
        [SerializeField] private float formDuration = 0.35f;
        [SerializeField] private float aimDuration = 0.45f;

        [Header("Shot")]
        [SerializeField] private float swordSpeed = 24f;
        [SerializeField] private float flightTime = 1.6f;
        [SerializeField] private float pathLength = 14f;

        public override bool CanUseSkill(GameObject target = null) =>
            Boss != null && Boss.Target != null && !Boss.IsDead;

        protected override IEnumerator ExecuteSwordmaster(GameObject target)
        {
            Boss.AttackReady(Boss.Target.position);
            Boss.PlayAnimation(Swordmaster.JumpState);
            Boss.Cue(SwordmasterCue.CrossfireReady, Boss.transform.position);

            for (int round = 0; round < roundCount && !Boss.IsDead; round++)
            {
                int minimum = Mathf.Max(1, Mathf.Min(swordsPerRound.x, swordsPerRound.y));
                int maximum = Mathf.Max(minimum, Mathf.Max(swordsPerRound.x, swordsPerRound.y));
                List<EnchantedSword> swords = Boss.TakeSwords(Random.Range(minimum, maximum + 1));
                List<Vector2> directions = new List<Vector2>();
                yield return FormAround(swords, directions);

                Boss.PlayAnimation(round % 2 == 0 ? Swordmaster.Attack1State : Swordmaster.Attack2State);
                Boss.Cue(SwordmasterCue.VolleyFire, Boss.transform.position);
                for (int i = 0; i < swords.Count; i++)
                    if (swords[i] != null) swords[i].FireMagic(directions[i], swordSpeed, flightTime, false);
                yield return new WaitForSeconds(roundGap / DurationScale);
            }

            if (Boss.IsDead) yield break;
            List<EnchantedSword> finalSwords = Boss.TakeSwords(finalSwordCount);
            List<Vector2> finalDirections = new List<Vector2>();
            yield return FormAround(finalSwords, finalDirections);
            for (int i = 0; i < finalSwords.Count && !Boss.IsDead; i++)
            {
                if (finalSwords[i] == null) continue;
                Boss.PlayAnimation(i % 2 == 0 ? Swordmaster.Attack1State : Swordmaster.Attack2State);
                Boss.Cue(SwordmasterCue.VolleyFire, finalSwords[i].transform.position);
                finalSwords[i].FireMagic(finalDirections[i], swordSpeed, flightTime, false);
                yield return new WaitForSeconds(finalFireInterval / DurationScale);
            }
        }

        private IEnumerator FormAround(List<EnchantedSword> swords, List<Vector2> directions)
        {
            Vector3 center = Boss.Target.position;
            float startAngle = Random.Range(0f, 360f);
            List<Vector3> points = new List<Vector3>();
            for (int i = 0; i < swords.Count; i++)
            {
                float angle = startAngle + 360f * i / Mathf.Max(1, swords.Count) + Random.Range(-25f, 25f);
                float radius = Random.Range(Mathf.Min(radiusRange.x, radiusRange.y), Mathf.Max(radiusRange.x, radiusRange.y));
                Vector3 point = center + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0f) * radius;
                point.z = Boss.transform.position.z;
                points.Add(point);
                Vector2 facing = (center - point).normalized;
                swords[i].MoveTo(point, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg, formDuration / DurationScale);
            }
            yield return new WaitForSeconds(formDuration / DurationScale);

            Vector3 aimPoint = Boss.Target != null ? Boss.Target.position : center;
            directions.Clear();
            Boss.Cue(SwordmasterCue.VolleyWarning, aimPoint);
            for (int i = 0; i < swords.Count; i++)
            {
                Vector2 direction = (aimPoint - points[i]);
                if (direction.sqrMagnitude < 0.0001f) direction = Vector2.down;
                direction.Normalize();
                directions.Add(direction);
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                swords[i].MoveTo(points[i], angle, 0.08f / DurationScale);
                swords[i].ShowPathLine(points[i], points[i] + (Vector3)(direction * pathLength), aimDuration / DurationScale);
            }
            yield return new WaitForSeconds(aimDuration / DurationScale);
        }
    }
}
