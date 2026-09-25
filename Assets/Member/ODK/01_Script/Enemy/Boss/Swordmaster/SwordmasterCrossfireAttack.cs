using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Swordmaster
{
    public class SwordmasterCrossfireAttack : SwordmasterSkill
    {
        [SerializeField] private float readyDuration = 1f;
        [SerializeField] private float scatterDuration = 0.45f;
        [SerializeField] private float lineWarningDuration = 0.5f;
        [SerializeField] private float rapidInterval = 0.07f;
        [SerializeField] private float lineSwordSpeed = 28f;
        [SerializeField] private float finalSpawnRadius = 3.5f;
        [SerializeField] private float finalWarningDuration = 0.55f;
        [SerializeField] private float finalSwordSpeed = 12f;

        protected override bool ReturnSwordsOnComplete => false;

        private readonly List<SwordmasterTelegraph> warnings = new List<SwordmasterTelegraph>();

        public override bool CanUseSkill(GameObject target = null) =>
            Boss != null && Boss.Target != null && !Boss.IsDead;

        protected override IEnumerator ExecuteSwordmaster(GameObject target)
        {
            Vector3 initialTarget = Boss.Target.position;
            Boss.AttackReady(initialTarget);
            yield return new WaitForSeconds(readyDuration / DurationScale);

            List<EnchantedSword> swords = Boss.TakeSwords(8);
            for (int i = 0; i < swords.Count; i++)
            {
                float angle = i / (float)Mathf.Max(1, swords.Count) * 360f;
                float radians = angle * Mathf.Deg2Rad;
                Vector3 scatterPoint = Boss.ArenaCenter + new Vector3(
                    Mathf.Cos(radians) * (Boss.ArenaHalfWidth - 1f),
                    Mathf.Sin(radians) * (Boss.ArenaHalfHeight - 1f),
                    Mathf.Sin(radians) * 0.8f
                );
                swords[i].MoveTo(scatterPoint, angle, scatterDuration / DurationScale);
            }
            yield return new WaitForSeconds(scatterDuration / DurationScale);

            yield return FireHorizontalVolley(swords);
            Boss.ReturnControlledSwords();
            yield return new WaitForSeconds(0.38f / DurationScale);

            yield return FireVerticalVolley();
            Boss.ReturnControlledSwords();
            yield return new WaitForSeconds(0.38f / DurationScale);

            yield return FireDispelledCross();
        }

        private IEnumerator FireHorizontalVolley(List<EnchantedSword> swords)
        {
            float y = Boss.Target.position.y;
            float minX = Boss.ArenaCenter.x - Boss.ArenaHalfWidth;
            float maxX = Boss.ArenaCenter.x + Boss.ArenaHalfWidth;
            SwordmasterTelegraph warning = CreateWarning(
                new Vector3(minX, y),
                new Vector3(maxX, y)
            );
            yield return new WaitForSeconds(lineWarningDuration / DurationScale);
            DestroyWarning(warning);

            float travelTime = Boss.ArenaHalfWidth * 2f / lineSwordSpeed + 0.18f;
            for (int i = 0; i < swords.Count; i++)
            {
                bool fromLeft = i % 2 == 0;
                Vector3 start = new Vector3(fromLeft ? minX : maxX, y, (i - 3.5f) * 0.08f);
                swords[i].MoveTo(start, fromLeft ? 0f : 180f, 0.02f);
                yield return new WaitForSeconds(0.025f / DurationScale);
                swords[i].FireMagic(fromLeft ? Vector2.right : Vector2.left, lineSwordSpeed, travelTime);
                yield return new WaitForSeconds(rapidInterval / DurationScale);
            }
            yield return new WaitForSeconds(travelTime / DurationScale);
        }

        private IEnumerator FireVerticalVolley()
        {
            float x = Boss.Target.position.x;
            float minY = Boss.ArenaCenter.y - Boss.ArenaHalfHeight;
            float maxY = Boss.ArenaCenter.y + Boss.ArenaHalfHeight;
            SwordmasterTelegraph warning = CreateWarning(
                new Vector3(x, minY),
                new Vector3(x, maxY)
            );
            yield return new WaitForSeconds(lineWarningDuration / DurationScale);
            DestroyWarning(warning);

            List<EnchantedSword> swords = Boss.TakeSwords(8);
            float travelTime = Boss.ArenaHalfHeight * 2f / lineSwordSpeed + 0.18f;
            for (int i = 0; i < swords.Count; i++)
            {
                bool fromBottom = i % 2 == 0;
                Vector3 start = new Vector3(x, fromBottom ? minY : maxY, (i - 3.5f) * 0.08f);
                swords[i].MoveTo(start, fromBottom ? 90f : -90f, 0.02f);
                yield return new WaitForSeconds(0.025f / DurationScale);
                swords[i].FireMagic(fromBottom ? Vector2.up : Vector2.down, lineSwordSpeed, travelTime);
                yield return new WaitForSeconds(rapidInterval / DurationScale);
            }
            yield return new WaitForSeconds(travelTime / DurationScale);
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

            foreach (EnchantedSword sword in swords)
            {
                if (sword == null) continue;
                Vector2 direction = (center - sword.transform.position).normalized;
                sword.FireDispelled(direction, finalSwordSpeed);
            }
            Boss.AttackImpact(center);
            yield return new WaitForSeconds(0.25f / DurationScale);
        }

        private SwordmasterTelegraph CreateWarning(Vector3 start, Vector3 end)
        {
            SwordmasterTelegraph warning = Boss.SpawnTelegraph();
            if (warning == null) return null;
            warning.Show(start, end, lineWarningDuration / DurationScale);
            warnings.Add(warning);
            return warning;
        }

        private void DestroyWarning(SwordmasterTelegraph warning)
        {
            if (warning == null) return;
            warnings.Remove(warning);
            Destroy(warning.gameObject);
        }

        private void ClearWarnings()
        {
            foreach (SwordmasterTelegraph warning in warnings)
                if (warning != null) Destroy(warning.gameObject);
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
