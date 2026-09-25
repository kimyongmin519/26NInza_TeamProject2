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

        public override bool CanUseSkill(GameObject target = null) => Boss != null && !Boss.IsDead;

        protected override IEnumerator ExecuteVolcanus(GameObject target)
        {
            int count = Boss.IsPhaseTwo ? phaseTwoCount : phaseOneCount;
            Boss.PoseVisual(
                new Vector2(0f, 0.2f),
                -6f,
                new Vector2(0.94f, 1.08f),
                0.25f / DurationScale,
                Ease.OutBack
            );
            for (int i = 0; i < count && !Boss.IsDead; i++)
            {
                float x = Random.Range(
                    Boss.ArenaCenter.x - Boss.ArenaHalfWidth + 0.8f,
                    Boss.ArenaCenter.x + Boss.ArenaHalfWidth - 0.8f
                );
                Vector3 groundPoint = Boss.GetGroundPoint(x);
                yield return ShowWarning(groundPoint);

                Vector3 spawnPosition = groundPoint + Vector3.up * spawnHeight;
                Boss.SpawnBoulder(
                    spawnPosition,
                    new Vector2(
                        Random.Range(-horizontalVelocity, horizontalVelocity),
                        -fallSpeed * Random.Range(0.85f, 1.2f)
                    )
                );
                float sway = i % 2 == 0 ? -1f : 1f;
                Boss.PoseVisual(
                    new Vector2(sway * 0.16f, 0.12f),
                    sway * 5f,
                    new Vector2(1.03f, 0.97f),
                    spawnInterval / DurationScale,
                    Ease.OutQuad
                );
                Boss.PlayFeedback(VolcanusFeedbackType.Boulder, spawnPosition);
                yield return new WaitForSeconds(spawnInterval / DurationScale);
            }
        }

        private IEnumerator ShowWarning(Vector3 groundPoint)
        {
            LineRenderer line = Boss.SpawnTelegraphLine();
            if (line == null)
            {
                Boss.AttackReady(groundPoint);
                yield return new WaitForSeconds(warningDuration / DurationScale);
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
                    warningDuration * 0.5f / DurationScale
                )
                .SetLoops(2, LoopType.Yoyo)
                .SetTarget(line);
            Boss.AttackReady(groundPoint);
            yield return new WaitForSeconds(warningDuration / DurationScale);
            pulse.Kill();
            Destroy(line.gameObject);
        }
    }
}
