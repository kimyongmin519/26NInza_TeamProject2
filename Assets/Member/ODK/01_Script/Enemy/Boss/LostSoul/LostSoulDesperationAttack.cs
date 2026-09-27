using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.LostSoul
{
    public class LostSoulDesperationAttack : LostSoulSkill
    {
        [Header("Movement / Slash")]
        [SerializeField] private float arenaPadding = 1.25f;
        [SerializeField] private float moveDuration = 0.24f;
        [SerializeField] private float slashReadyTime = 0.12f;

        [Header("Weak Soul Stream")]
        [SerializeField] private float weakSoulInterval = 0.34f;
        [SerializeField] private float weakSoulSpeed = 8.5f;
        [SerializeField] private float weakSoulSpawnMargin = 1.2f;

        [Header("Radial Slash")]
        [SerializeField] private float slashWarningDuration = 0.42f;
        [SerializeField] private float slashActiveDuration = 0.16f;
        [SerializeField] private float slashWidth = 0.52f;
        [SerializeField] private float slashDamage = 1f;
        [SerializeField] private Color slashColor = new Color(0.82f, 0.24f, 1f, 1f);

        [Header("Health Pressure")]
        [SerializeField, Range(0f, 1f)] private float rotatingSlashHealth = 0.2f;
        [SerializeField] private float rotatingDegrees = 145f;
        [SerializeField] private float cycleIntervalAt25 = 0.72f;
        [SerializeField] private float cycleIntervalAt10 = 0.34f;

        [Header("Enrage")]
        [SerializeField, Range(0f, 1f)] private float enrageHealth = 0.1f;
        [SerializeField] private float enrageSlashInterval = 0.055f;
        [SerializeField] private float enrageWarningDuration = 0.2f;
        [SerializeField] private float enrageMinimumDistance = 0.8f;
        [SerializeField] private float enrageMaximumDistance = 8.5f;

        private Coroutine weakSoulRoutine;
        private bool running;
        private bool frenzyAnimationPlaying;

        protected override IEnumerator ExecuteLostSoul(GameObject target)
        {
            running = true;
            frenzyAnimationPlaying = false;
            Boss.PlayDesperationScreamFeedback();
            weakSoulRoutine = StartCoroutine(WeakSoulStream(target.transform));

            while (running && Boss != null && Boss.IsPhaseTwo && !Boss.IsDead && target != null)
            {
                float healthRatio = Boss.HealthRatio;
                if (healthRatio <= enrageHealth)
                    yield return EnrageCycle(target.transform);
                else
                    yield return NormalCycle(target.transform);
            }

            StopWeakSoulStream();
        }

        private IEnumerator NormalCycle(Transform target)
        {
            frenzyAnimationPlaying = false;
            Vector3 destination = GetRandomGroundPosition();
            Boss.transform.DOKill();
            yield return Boss.transform.DOMove(destination, moveDuration / DurationScale)
                .SetEase(Ease.InOutSine)
                .WaitForCompletion();

            if (!running || target == null) yield break;
            float readyTime = slashReadyTime / DurationScale;
            float warningTime = slashWarningDuration / DurationScale;
            SwingAt(readyTime + warningTime);
            Boss.AttackReady(target.position);
            yield return new WaitForSeconds(readyTime);

            int directionCount = GetDirectionCount();
            bool rotating = Boss.HealthRatio <= rotatingSlashHealth;
            SpawnRadialSlashes(target.position, directionCount, rotating);

            float waitDuration = Mathf.Lerp(
                cycleIntervalAt10,
                cycleIntervalAt25,
                Mathf.InverseLerp(enrageHealth, 0.25f, Boss.HealthRatio)
            ) / DurationScale;
            waitDuration = Mathf.Max(waitDuration, warningTime + slashActiveDuration * 0.5f / DurationScale);
            yield return WaitCycle(target, waitDuration);
        }

        private IEnumerator EnrageCycle(Transform target)
        {
            if (!frenzyAnimationPlaying)
            {
                Boss.PlayAnimation("frenzy", 0.03f);
                frenzyAnimationPlaying = true;
            }
            Boss.transform.position = target.position + new Vector3(Random.Range(-10f, 10f), Random.Range(-5f, 5f));
            Vector3 center = GetRandomSlashPosition(target.position);
            float angle = Random.Range(0f, 180f);
            SpawnSlash(center, angle, enrageWarningDuration, 0f);
            yield return new WaitForSeconds(enrageSlashInterval / DurationScale);
        }

        private IEnumerator WaitCycle(Transform target, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration && running && target != null)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator WeakSoulStream(Transform target)
        {
            while (running && Boss != null && !Boss.IsDead && target != null)
            {
                Vector3 spawn = GetRandomWeakSoulSpawn();
                Vector2 direction = ((Vector2)target.position - (Vector2)spawn).normalized;
                Boss.SpawnWeakSoul(spawn, direction * weakSoulSpeed);
                yield return new WaitForSeconds(weakSoulInterval / DurationScale);
            }
        }

        private void SpawnRadialSlashes(Vector3 center, int count, bool rotating)
        {
            float startAngle = Random.Range(0f, 180f);
            float step = 180f / Mathf.Max(1, count);
            float rotation = rotating
                ? (Random.value < 0.5f ? -rotatingDegrees : rotatingDegrees)
                : 0f;
            for (int i = 0; i < count; i++)
                SpawnSlash(center, startAngle + step * i, slashWarningDuration, rotation);
        }

        private void SpawnSlash(Vector3 center, float angle, float warningDuration, float preFireRotationDegrees)
        {
            float radians = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            float length = Mathf.Max(Boss.ArenaHalfWidth, Boss.ArenaHalfHeight) * 3.2f;
            Vector3 origin = center - (Vector3)direction * (length * 0.5f);
            Boss.SpawnSlashBeam(
                origin,
                direction,
                length,
                slashWidth,
                warningDuration / DurationScale,
                slashActiveDuration / DurationScale,
                slashDamage,
                slashColor,
                preFireRotationDegrees
            );
        }

        private int GetDirectionCount()
        {
            float pressure = Mathf.InverseLerp(0.25f, enrageHealth, Boss.HealthRatio);
            float lowHealthBias = Mathf.Lerp(2.4f, 0.55f, pressure);
            float roll = Mathf.Pow(Random.value, lowHealthBias);
            return Mathf.Clamp(3 + Mathf.FloorToInt(roll * 6f), 3, 8);
        }

        private Vector3 GetRandomGroundPosition()
        {
            float extent = Mathf.Max(0.25f, Boss.ArenaHalfWidth - arenaPadding);
            float x = Random.Range(Boss.ArenaCenter.x - extent, Boss.ArenaCenter.x + extent);
            Vector3 point = Boss.GetGroundPoint(x) + Vector3.up * 0.05f;
            point.z = Boss.transform.position.z;
            return point;
        }

        private Vector3 GetRandomWeakSoulSpawn()
        {
            Vector3 center = Boss.ArenaCenter;
            float left = center.x - Boss.ArenaHalfWidth - weakSoulSpawnMargin;
            float right = center.x + Boss.ArenaHalfWidth + weakSoulSpawnMargin;
            float bottom = center.y - Boss.ArenaHalfHeight * 0.15f;
            float top = center.y + Boss.ArenaHalfHeight + weakSoulSpawnMargin;
            int edge = Random.Range(0, 3);
            return edge switch
            {
                0 => new Vector3(left, Random.Range(bottom, top), Boss.transform.position.z),
                1 => new Vector3(right, Random.Range(bottom, top), Boss.transform.position.z),
                _ => new Vector3(Random.Range(left, right), top, Boss.transform.position.z)
            };
        }

        private Vector3 GetRandomSlashPosition(Vector3 targetPosition)
        {
            float maximum = Mathf.Max(enrageMinimumDistance, enrageMaximumDistance);
            float distance = Random.Range(Mathf.Max(0f, enrageMinimumDistance), maximum);
            Vector2 offset = Random.insideUnitCircle.normalized * distance;
            Vector3 center = targetPosition + (Vector3)offset;
            center.x = Mathf.Clamp(
                center.x,
                Boss.ArenaCenter.x - Boss.ArenaHalfWidth,
                Boss.ArenaCenter.x + Boss.ArenaHalfWidth
            );
            center.y = Mathf.Clamp(
                center.y,
                Boss.ArenaCenter.y - Boss.ArenaHalfHeight,
                Boss.ArenaCenter.y + Boss.ArenaHalfHeight
            );
            center.z = Boss.transform.position.z;
            return center;
        }

        private void StopWeakSoulStream()
        {
            running = false;
            if (weakSoulRoutine == null) return;
            StopCoroutine(weakSoulRoutine);
            weakSoulRoutine = null;
        }

        protected override void OnLostSoulCompleted()
        {
            StopWeakSoulStream();
            Boss?.transform.DOKill();
        }

        protected override void OnLostSoulCancelled()
        {
            StopWeakSoulStream();
            Boss?.transform.DOKill();
        }
    }
}
