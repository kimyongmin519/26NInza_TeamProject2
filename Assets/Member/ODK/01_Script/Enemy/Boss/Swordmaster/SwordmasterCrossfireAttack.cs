using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Swordmaster
{
    public class SwordmasterCrossfireAttack : SwordmasterSkill
    {
        [Header("Ready")]
        [SerializeField] private float readyDuration = 0.7f;
        [SerializeField] private float turnGap = 0.35f;

        [Header("Turn 1-2 Sword Wall")]
        [SerializeField, Min(2)] private int wallSwordCount = 7;
        [SerializeField] private float wallDistance = 9f;
        [SerializeField] private float wallHalfSpan = 6f;
        [SerializeField] private float rowHeight = 7f;
        [SerializeField] private float rowSwordSpeed = 58f;
        [SerializeField] private float wallFormDuration = 0.32f;
        [SerializeField] private float wallHoldDuration = 0.38f;
        [SerializeField] private float wallSwordSpeed = 36f;
        [SerializeField] private float wallFireStagger = 0.03f;

        [Header("Turn 3-4 Axis Alternating")]
        [SerializeField, Min(1)] private int axisShotCount = 6;
        [SerializeField] private float axisFormDuration = 0.14f;
        [SerializeField] private float axisAimDuration = 0.26f;
        [SerializeField] private float axisInterval = 0.1f;
        [SerializeField] private float axisSwordSpeed = 30f;
        [SerializeField] private float axisDistance = 7f;

        [Header("Turn 5 Final Cross")]
        [SerializeField] private float lineWarningDuration = 0.5f;
        [SerializeField, Min(0.01f)] private float swordPathWidth = 0.08f;
        [SerializeField] private float finalSpawnRadius = 6.5f;
        [SerializeField] private float finalWarningDuration = 0.55f;
        [SerializeField] private float finalSwordSpeed = 15f;

        protected override bool ReturnSwordsOnComplete => false;

        private readonly List<SwordmasterTelegraph> warnings = new List<SwordmasterTelegraph>();

        private float MinX => Boss.ArenaCenter.x - Boss.ArenaHalfWidth;
        private float MaxX => Boss.ArenaCenter.x + Boss.ArenaHalfWidth;
        private float MinY => Boss.ArenaCenter.y - Boss.ArenaHalfHeight;
        private float MaxY => Boss.ArenaCenter.y + Boss.ArenaHalfHeight;

        public override bool CanUseSkill(GameObject target = null) =>
            Boss != null && Boss.Target != null && !Boss.IsDead;

        protected override IEnumerator ExecuteSwordmaster(GameObject target)
        {
            Boss.AttackReady(Boss.Target.position);
            Boss.PlayAnimation(Swordmaster.JumpState);
            Boss.Cue(SwordmasterCue.CrossfireReady, Boss.transform.position);
            yield return Wait(readyDuration);

            yield return FireSwordWall(true);
            yield return Wait(turnGap);
            if (Boss.IsDead) yield break;

            yield return FireSwordWall(false);
            yield return Wait(turnGap);
            if (Boss.IsDead) yield break;

            yield return FireAxisAlternating(true);
            yield return Wait(turnGap);
            if (Boss.IsDead) yield break;

            yield return FireAxisAlternating(false);
            yield return Wait(turnGap);
            if (Boss.IsDead) yield break;

            yield return FireDispelledCross();
        }

        private WaitForSeconds Wait(float seconds) => new WaitForSeconds(Mathf.Max(0f, seconds) / DurationScale);

        private IEnumerator FireSwordWall(bool verticalColumn)
        {
            List<EnchantedSword> swords = Boss.TakeSwords(wallSwordCount);
            if (swords.Count == 0) yield break;

            bool fromNegative = Random.value < 0.5f;
            Vector2 direction;
            float faceAngle;
            if (verticalColumn)
            {
                direction = fromNegative ? Vector2.right : Vector2.left;
                faceAngle = fromNegative ? 0f : 180f;
            }
            else
            {
                direction = Vector2.down;
                faceAngle = -90f;
            }

            Vector3 player = Boss.Target != null ? Boss.Target.position : Boss.transform.position;
            float center = verticalColumn ? player.y : player.x;
            float spanMin = center - wallHalfSpan;
            float spanMax = center + wallHalfSpan;
            float spacing = (spanMax - spanMin) / swords.Count;
            float offset = Random.Range(0.2f, 0.8f) * spacing;
            float z = Boss.transform.position.z;

            Boss.PlayAnimation(Swordmaster.JumpState);
            Boss.Cue(SwordmasterCue.VolleyWarning, Boss.transform.position);
            for (int i = 0; i < swords.Count; i++)
            {
                float along = spanMin + offset + spacing * i;
                Vector3 formPoint = verticalColumn
                    ? new Vector3(player.x + (fromNegative ? -wallDistance : wallDistance), along, z)
                    : new Vector3(along, player.y + rowHeight, z);
                swords[i].MoveTo(formPoint, faceAngle, wallFormDuration / DurationScale);
            }
            yield return Wait(wallFormDuration + wallHoldDuration);

            Boss.PlayAnimation(Swordmaster.Attack1State);
            Boss.Cue(SwordmasterCue.VolleyFire, Boss.transform.position);
            float speed = verticalColumn ? wallSwordSpeed : rowSwordSpeed;
            float travel = (verticalColumn ? wallDistance : rowHeight) * 2f / speed + 0.2f;
            foreach (EnchantedSword sword in swords)
            {
                if (sword == null) continue;
                sword.FireMagic(direction, speed, travel, false);
                if (wallFireStagger > 0f) yield return Wait(wallFireStagger);
            }
            yield return Wait(travel * 0.5f);
        }

        private IEnumerator FireAxisAlternating(bool alongPlayerX)
        {
            float z = Boss.transform.position.z;
            for (int shot = 0; shot < axisShotCount && !Boss.IsDead && Boss.Target != null; shot++)
            {
                List<EnchantedSword> taken = Boss.TakeSwords(1);
                if (taken.Count == 0) yield break;
                EnchantedSword sword = taken[0];

                bool firstSide = shot % 2 == 0;
                Vector3 player = Boss.Target.position;
                Vector3 start;
                Vector3 end;
                if (alongPlayerX)
                {
                    Vector3 top = new Vector3(player.x, player.y + axisDistance, z);
                    Vector3 bottom = new Vector3(player.x, player.y - axisDistance, z);
                    start = firstSide ? top : bottom;
                    end = firstSide ? bottom : top;
                }
                else
                {
                    Vector3 left = new Vector3(player.x - axisDistance, player.y, z);
                    Vector3 right = new Vector3(player.x + axisDistance, player.y, z);
                    start = firstSide ? left : right;
                    end = firstSide ? right : left;
                }

                Vector2 direction = (end - start).normalized;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                float travel = Vector3.Distance(start, end) / axisSwordSpeed + 0.15f;
                sword.MoveTo(start, angle, axisFormDuration / DurationScale);
                sword.ShowPathLine(start, end, (axisFormDuration + axisAimDuration) / DurationScale);
                Boss.Cue(SwordmasterCue.VolleyWarning, start);
                yield return Wait(axisFormDuration + axisAimDuration);

                Boss.PlayAnimation(firstSide ? Swordmaster.Attack1State : Swordmaster.Attack2State);
                Boss.Cue(SwordmasterCue.VolleyFire, start);
                sword.FireMagic(direction, axisSwordSpeed, travel);
                yield return Wait(axisInterval);
            }
        }

        private IEnumerator FireDispelledCross()
        {
            Vector3 center = Boss.Target.position;
            List<EnchantedSword> swords = Boss.TakeSwords(8);
            for (int i = 0; i < swords.Count; i++)
            {
                float angle = i < 4 ? i * 90f : 45f + (i - 4) * 90f;
                float radians = angle * Mathf.Deg2Rad;
                Vector2 outward = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                Vector3 spawnPoint = center + (Vector3)(outward * finalSpawnRadius);
                float faceAngle = Mathf.Atan2(-outward.y, -outward.x) * Mathf.Rad2Deg;
                swords[i].MoveTo(spawnPoint, faceAngle, 0.28f / DurationScale);
            }

            CreateWarning(
                center + Vector3.left * finalSpawnRadius,
                center + Vector3.right * finalSpawnRadius
            );
            CreateWarning(
                center + Vector3.down * finalSpawnRadius,
                center + Vector3.up * finalSpawnRadius
            );
            CreateWarning(
                center + new Vector3(-finalSpawnRadius, -finalSpawnRadius),
                center + new Vector3(finalSpawnRadius, finalSpawnRadius)
            );
            CreateWarning(
                center + new Vector3(-finalSpawnRadius, finalSpawnRadius),
                center + new Vector3(finalSpawnRadius, -finalSpawnRadius)
            );
            yield return new WaitForSeconds(finalWarningDuration / DurationScale);
            ClearWarnings();

            float meetDistance = 0f;
            foreach (EnchantedSword sword in swords)
            {
                if (sword == null) continue;
                Vector2 toCenter = center - sword.transform.position;
                meetDistance = Mathf.Max(meetDistance, toCenter.magnitude);
                sword.FireDispelled(toCenter.normalized, finalSwordSpeed);
            }
            Boss.PlayAnimation(Swordmaster.Attack2State);
            float meetTime = finalSwordSpeed > 0f ? meetDistance / finalSwordSpeed : 0f;
            if (meetTime > 0f) yield return new WaitForSeconds(meetTime);
            if (Boss.IsDead) yield break;
            Boss.AttackImpact(center);
            Boss.Cue(SwordmasterCue.FinalCross, center);
            yield return new WaitForSeconds(0.25f / DurationScale);
        }

        private SwordmasterTelegraph CreateWarning(Vector3 start, Vector3 end)
        {
            SwordmasterTelegraph warning = Boss.SpawnTelegraph();
            if (warning == null) return null;
            warning.Show(start, end, lineWarningDuration / DurationScale, swordPathWidth);
            warnings.Add(warning);
            Boss.Cue(SwordmasterCue.VolleyWarning, Vector3.Lerp(start, end, 0.5f));
            return warning;
        }

        private void DestroyWarning(SwordmasterTelegraph warning)
        {
            if (warning == null) return;
            warnings.Remove(warning);
            Boss.ReleaseTelegraph(warning);
        }

        private void ClearWarnings()
        {
            foreach (SwordmasterTelegraph warning in warnings)
                if (warning != null) Boss?.ReleaseTelegraph(warning);
            warnings.Clear();
        }

        protected override void OnSwordmasterCancel()
        {
            ClearWarnings();
        }

        private void OnDrawGizmosSelected()
        {
            Swordmaster boss = GetComponentInParent<Swordmaster>();
            if (boss == null) return;
            Vector3 center = boss.ArenaCenter;
            Gizmos.color = new Color(1f, 0.15f, 0.45f, 0.85f);
            Gizmos.DrawLine(center + Vector3.left * boss.ArenaHalfWidth, center + Vector3.right * boss.ArenaHalfWidth);
            Gizmos.DrawLine(center + Vector3.down * boss.ArenaHalfHeight, center + Vector3.up * boss.ArenaHalfHeight);
            Gizmos.color = new Color(0.75f, 0.25f, 1f, 0.75f);
            Gizmos.DrawWireSphere(center, finalSpawnRadius);
        }
    }
}
