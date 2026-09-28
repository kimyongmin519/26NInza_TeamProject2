using System.Collections;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus.Legacy
{
    public class VolcanusFiveSlamAttack : VolcanusSkill
    {
        [SerializeField] private int slamCount = 5;
        [SerializeField] private float slamWidthRate = 0.75f;
        [SerializeField] private float readyHeight = 8f;
        [SerializeField] private float readyDuration = 0.42f;
        [SerializeField] private float slamDuration = 0.16f;
        [SerializeField] private float slamInterval = 0.25f;
        [SerializeField] private float damage = 42f;
        [SerializeField, Range(0.2f, 1.2f)] private float hitRadiusRatio = 0.65f;

        private Sequence sequence;

        public override bool CanUseSkill(GameObject target = null)
        {
            return Boss != null && Boss.RightHand != null && Boss.RightHand.IsUseable();
        }

        protected override IEnumerator Execute(GameObject target)
        {
            bool startFromRight = Random.value < 0.5f;
            float slamWidth = Boss.ArenaHalfWidth * slamWidthRate;
            float startX = Boss.ArenaCenter.x + (startFromRight ? slamWidth : -slamWidth);
            float endX = Boss.ArenaCenter.x - (startFromRight ? slamWidth : -slamWidth);
            Transform fist = Boss.RightHand.transform;

            Boss.RightHand.SetAnotherMoving(true);

            for (int i = 0; i < slamCount && !Boss.IsDead; i++)
            {
                float rate = slamCount <= 1 ? 0f : (float)i / (slamCount - 1);
                Vector3 groundPoint = Boss.GetGroundPoint(Mathf.Lerp(startX, endX, rate));
                Vector3 readyPosition = groundPoint + Vector3.up * readyHeight;
                readyPosition.z = fist.position.z;

                Boss.AttackReady(groundPoint);
                Boss.ReactPieces(Boss.RightHand, Vector2.down);
                sequence = DOTween.Sequence();
                sequence.Append(fist.DOMove(readyPosition, Scaled(readyDuration)).SetEase(Ease.OutBack));
                sequence.Join(fist.DORotate(new Vector3(0f, 0f, Boss.FistDownAngle), Scaled(readyDuration)).SetEase(Ease.OutQuad));
                yield return sequence.WaitForCompletion();

                Vector3 hitPosition = Boss.GetImpactVisualPosition(fist, groundPoint, Boss.FistImpactVisualOffset);
                PlayAttackAnimation(Boss.RightHand);
                StartCoroutine(StrikeWindow(DamageCaster, Volcanus.StrikePart.Fist,
                    Scaled(slamDuration) + 0.06f, damage, DamageType.Melee, hitRadiusRatio));

                sequence = DOTween.Sequence();
                sequence.Append(fist.DOMove(hitPosition, Scaled(slamDuration)).SetEase(Ease.InExpo));
                yield return sequence.WaitForCompletion();
                Boss.AttackImpact(groundPoint);
                Boss.SpawnFistRocks(groundPoint);

                if (i < slamCount - 1)
                {
                    yield return new WaitForSeconds(Scaled(slamInterval));
                    EndAttackAnimation();
                }
            }

            DamageCaster?.DisableCasting();
            Boss.RightHand.SetAnotherMoving(false);
        }

        protected override void OnVolcanusCancel()
        {
            sequence?.Kill();
            DamageCaster?.DisableCasting();
            if (Boss != null && Boss.RightHand != null && Boss.RightHand.IsAnotherMoving)
                Boss.RightHand.SetAnotherMoving(false);
        }
    }
}
