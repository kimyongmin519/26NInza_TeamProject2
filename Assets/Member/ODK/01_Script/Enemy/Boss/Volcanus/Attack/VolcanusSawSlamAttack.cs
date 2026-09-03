using System.Collections;
using DG.Tweening;
using UnityEngine;

public class VolcanusSawSlamAttack : VolcanusSkill
{
    [SerializeField] private float readyHeight = 9f;
    [SerializeField] private float readyDuration = 0.75f;
    [SerializeField] private float slamDuration = 0.16f;
    [SerializeField] private float waitDuration = 0.45f;
    [SerializeField] private float damage = 110f;

    private Sequence sequence;

    public override bool CanUseSkill(GameObject target = null)
    {
        return Boss != null && Boss.Saw != null && Boss.LeftHand != null && Boss.LeftHand.IsUseable();
    }

    protected override IEnumerator Execute(GameObject target)
    {
        Boss.SetSawMoving(true);
        Vector3 groundPoint = Boss.GetGroundPoint(Boss.Target.position.x);
        Vector3 readyPosition = Boss.GetSawImpactPosition(groundPoint) + Vector3.up * readyHeight;
        Boss.AttackReady(groundPoint);
        Boss.ReactPieces(Boss.LeftHand, Vector2.down);

        sequence = DOTween.Sequence();
        sequence.Append(Boss.Saw.DOMove(readyPosition, readyDuration * DurationScale).SetEase(Ease.OutBack));
        sequence.Join(Boss.Saw.DOLocalRotate(new Vector3(0f, 0f, Boss.SawDownAngle), readyDuration * DurationScale).SetEase(Ease.OutCubic));
        yield return sequence.WaitForCompletion();
        yield return new WaitForSeconds(waitDuration * DurationScale);

        groundPoint = Boss.GetGroundPoint(Boss.Target.position.x);
        Vector3 hitPosition = Boss.GetSawImpactPosition(groundPoint) + new Vector3(0,-3f,0);
        Vector3 damagePoint = groundPoint;
        PlayAttackAnimation(Boss.LeftHand);
        DamageCaster?.EnableCasting(
            new DamageData(damage, DamageType.Melee),
            (slamDuration + 0.16f) * DurationScale + 0.3f
        );
        sequence = DOTween.Sequence();
        sequence.Append(Boss.Saw.DOMove(hitPosition, slamDuration * DurationScale).SetEase(Ease.InExpo));
        sequence.AppendInterval(0.16f * DurationScale);
        yield return sequence.WaitForCompletion();
        Boss.AttackImpact(damagePoint);
        Boss.SpawnSawRocks(damagePoint);
        yield return new WaitForSeconds(0.3f);
        DamageCaster?.DisableCasting();
        EndAttackAnimation();
        Boss.SetSawMoving(false);
    }

    protected override void OnCancel()
    {
        sequence?.Kill();
        DamageCaster?.DisableCasting();
        if (Boss != null) Boss.SetSawMoving(false);
    }
}
