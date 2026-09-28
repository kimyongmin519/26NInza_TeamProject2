using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public class GolemBoulderRainAttack : VolcanusSkill
    {
        [SerializeField] private int phaseOneCount = 8;
        [SerializeField] private int phaseTwoCount = 12;
        [SerializeField] private float warningDuration = 0.28f;
        [SerializeField] private float spawnInterval = 0.12f;
        [SerializeField] private float spawnHeight = 8f;
        [SerializeField] private float fallSpeed = 3f;
        [SerializeField] private float horizontalVelocity = 1.5f;

        protected override string DefaultActionState => Volcanus.ComboState;

        public override bool CanUseSkill(GameObject target = null) => Boss != null && !Boss.IsDead;

        protected override IEnumerator ExecuteVolcanus(GameObject target)
        {
            int total = Boss.IsPhaseTwo ? phaseTwoCount : phaseOneCount;
            int spawned = 0;
            Boss.PlayFeedback(VolcanusFeedbackType.Ready, Boss.transform.position);

            while (spawned < total && !Boss.IsDead)
            {
                int impactCount = Mathf.Max(1, Boss.GetImpactTimes(ActionState, ActionSpeed).Length);
                int perImpact = Mathf.CeilToInt(total / (float)impactCount);
                yield return PerformAction(ActionState, (index, rate) =>
                {
                    int count = Mathf.Min(perImpact, total - spawned);
                    Punch(count, index);
                    spawned += count;
                });
            }

            yield return new WaitForSeconds((warningDuration + spawnInterval * 2f) / ActionSpeed);
        }

        private void Punch(int count, int punchIndex)
        {
            if (count <= 0) return;
            Vector3 fist = Boss.GetGroundPoint(Boss.transform.position.x + Boss.FacingToTarget() * 1.4f * Scale);
            Boss.ImpactVisual(Vector2.down, 0.14f * Scale, 0.16f / ActionSpeed);
            Boss.Shake(false);
            Boss.PlayFeedback(VolcanusFeedbackType.Step, fist);
            Boss.AttackImpact(fist);

            for (int i = 0; i < count; i++)
            {
                float x = Random.Range(
                    Boss.ArenaCenter.x - Boss.ArenaHalfWidth + 0.8f,
                    Boss.ArenaCenter.x + Boss.ArenaHalfWidth - 0.8f
                );
                if (i == 0 && punchIndex % 2 == 0 && Boss.Target != null)
                    x = ClampToArena(Boss.Target.position.x, 0.8f / Mathf.Max(0.01f, Scale));
                StartCoroutine(DropBoulder(Boss.GetGroundPoint(x), i * spawnInterval / ActionSpeed));
            }
        }

        private IEnumerator DropBoulder(Vector3 groundPoint, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            yield return ShowWarning(groundPoint);
            if (Boss == null || Boss.IsDead) yield break;

            Vector3 spawnPosition = groundPoint + Vector3.up * spawnHeight;
            Boss.SpawnBoulder(
                spawnPosition,
                new Vector2(
                    Random.Range(-horizontalVelocity, horizontalVelocity),
                    -fallSpeed * Random.Range(0.85f, 1.2f)
                )
            );
            Boss.PlayFeedback(VolcanusFeedbackType.Boulder, spawnPosition);
        }

        private IEnumerator ShowWarning(Vector3 groundPoint)
        {
            LineRenderer line = Boss.SpawnTelegraphLine();
            if (line == null)
            {
                Boss.AttackReady(groundPoint);
                yield return new WaitForSeconds(warningDuration / ActionSpeed);
                yield break;
            }
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, groundPoint);
            line.SetPosition(1, groundPoint + Vector3.up * spawnHeight);
            line.widthMultiplier = 0.07f;
            line.startColor = new Color(1f, 0.3f, 0.05f, 0.9f);
            line.endColor = new Color(1f, 0.75f, 0.1f, 0.45f);
            line.sortingOrder = 40;
            line.enabled = true;
            Tween pulse = DOTween.To(
                    () => line.widthMultiplier,
                    value => line.widthMultiplier = value,
                    0.18f,
                    warningDuration * 0.5f / ActionSpeed
                )
                .SetLoops(2, LoopType.Yoyo)
                .SetTarget(line);
            Boss.AttackReady(groundPoint);
            yield return new WaitForSeconds(warningDuration / ActionSpeed);
            pulse.Kill();
            if (line != null) Destroy(line.gameObject);
        }
    }
}
