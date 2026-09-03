using System.Collections;
using DG.Tweening;
using UnityEngine;

public class VolcanusFiveSlamAttack : VolcanusSkill
{
    [SerializeField] private int slamCount = 5;
    [SerializeField] private float slamWidthRate = 0.75f;
    [SerializeField] private float readyHeight = 8f;
    [SerializeField] private float readyDuration = 0.42f;
    [SerializeField] private float slamDuration = 0.16f;
    [SerializeField] private float slamInterval = 0.25f;
    [SerializeField] private float damage = 42f;

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

        for (int i = 0; i < slamCount; i++)
        {
            float rate = slamCount <= 1 ? 0f : (float)i / (slamCount - 1);
            Vector3 groundPoint = Boss.GetGroundPoint(Mathf.Lerp(startX, endX, rate));
            Vector3 readyPosition = groundPoint + Vector3.up * readyHeight;
            Vector3 hitPosition = Boss.GetImpactVisualPosition(fist, groundPoint, Boss.FistImpactVisualOffset);
            Vector3 damagePoint = groundPoint;

            Boss.AttackReady(groundPoint);
            Boss.ReactPieces(Boss.RightHand, Vector2.down);
            sequence = DOTween.Sequence();
            sequence.Append(fist.DOMove(readyPosition, readyDuration * DurationScale).SetEase(Ease.OutBack));
            sequence.Join(fist.DORotate(new Vector3(0f, 0f, Boss.FistDownAngle), readyDuration * DurationScale).SetEase(Ease.OutQuad));
            yield return sequence.WaitForCompletion();

            PlayAttackAnimation(Boss.RightHand);
            DamageCaster?.EnableCasting(
                new DamageData(damage, DamageType.Melee),
                slamDuration * DurationScale + Time.fixedDeltaTime
            );

            sequence = DOTween.Sequence();
            sequence.Append(fist.DOMove(hitPosition, slamDuration * DurationScale).SetEase(Ease.InExpo));
            yield return sequence.WaitForCompletion();
            Boss.AttackImpact(damagePoint);
            Boss.SpawnFistRocks(damagePoint);
            DamageCaster?.DisableCasting();
            

            if (i < slamCount - 1)
            {
                yield return new WaitForSeconds(slamInterval * DurationScale);
                EndAttackAnimation();
            }
        }

        Boss.RightHand.SetAnotherMoving(false);
    }

    protected override void OnCancel()
    {
        sequence?.Kill();
        DamageCaster?.DisableCasting();
        if (Boss != null && Boss.RightHand != null && Boss.RightHand.IsAnotherMoving)
            Boss.RightHand.SetAnotherMoving(false);
    }
}
