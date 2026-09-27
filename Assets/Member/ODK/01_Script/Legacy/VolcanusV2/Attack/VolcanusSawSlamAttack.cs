using System.Collections;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus.Legacy
{
    public class VolcanusSawSlamAttack : VolcanusSkill
    {
        [SerializeField] private float readyHeight = 9f;
        [SerializeField] private float readyDuration = 0.75f;
        [SerializeField] private float lockDuration = 0.14f;
        [SerializeField] private float slamDuration = 0.16f;
        [SerializeField] private float waitDuration = 0.45f;
        [SerializeField] private float damage = 110f;
        [SerializeField, Range(0.2f, 1.2f)] private float hitRadiusRatio = 0.65f;

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
            sequence.Append(Boss.Saw.DOMove(readyPosition, Scaled(readyDuration)).SetEase(Ease.OutBack));
            sequence.Join(Boss.Saw.DOLocalRotate(new Vector3(0f, 0f, Boss.SawDownAngle), Scaled(readyDuration)).SetEase(Ease.OutCubic));
            yield return sequence.WaitForCompletion();

            Vector3 lockedPoint = Boss.GetGroundPoint(Boss.Target.position.x);
            Vector3 hoverPosition = Boss.GetSawImpactPosition(lockedPoint) + Vector3.up * readyHeight;
            Boss.AttackReady(lockedPoint);
            sequence = DOTween.Sequence();
            sequence.Append(Boss.Saw.DOMove(hoverPosition, Scaled(lockDuration)).SetEase(Ease.OutCubic));
            yield return sequence.WaitForCompletion();
            yield return new WaitForSeconds(Scaled(waitDuration));

            Vector3 hitPosition = Boss.GetSawImpactPosition(lockedPoint);
            PlayAttackAnimation(Boss.LeftHand);
            float strikeTime = Scaled(slamDuration) + Scaled(0.16f) + 0.2f;
            StartCoroutine(StrikeWindow(DamageCaster, Volcanus.StrikePart.Saw, strikeTime, damage, DamageType.Melee, hitRadiusRatio));
            sequence = DOTween.Sequence();
            sequence.Append(Boss.Saw.DOMove(hitPosition, Scaled(slamDuration)).SetEase(Ease.InExpo));
            sequence.AppendInterval(Scaled(0.16f));
            yield return sequence.WaitForCompletion();
            Boss.AttackImpact(lockedPoint);
            Boss.SpawnSawRocks(lockedPoint);
            yield return new WaitForSeconds(Scaled(0.3f));
            DamageCaster?.DisableCasting();
            EndAttackAnimation();
            Boss.SetSawMoving(false);
        }

        protected override void OnVolcanusCancel()
        {
            sequence?.Kill();
            DamageCaster?.DisableCasting();
            if (Boss != null) Boss.SetSawMoving(false);
        }
    }
}
