using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public class VolcanusPunchAttack : VolcanusSkill
    {
        [SerializeField] private Vector2 dir = Vector2.right;
        [SerializeField] private float readyDuration = 0.55f;
        [SerializeField] private float punchDuration = 0.2f;
        [SerializeField] private float readyHeight = 2f;
        [SerializeField] private float damage = 75f;

        private Sequence sequence;

        public override bool CanUseSkill(GameObject target = null)
        {
            return Boss != null && Boss.RightHand != null && Boss.RightHand.IsUseable();
        }

        protected override IEnumerator Execute(GameObject target)
        {
            VolcanusPiece handPiece = Boss.RightHand;
            Transform hand = handPiece.transform;
            float direction = Mathf.Sign(dir.x);
            float groundY = Boss.GetGroundPoint(Boss.Target.position.x).y;
            Vector3 readyPosition = new Vector3(Boss.ArenaCenter.x - Boss.ArenaHalfWidth * direction, groundY + readyHeight, hand.position.z);
            Vector3 punchPosition = new Vector3(Boss.ArenaCenter.x + Boss.ArenaHalfWidth * direction, readyPosition.y, hand.position.z);
            float punchAngle = (Boss.FistDownAngle - 45f) + direction * 90f;

            handPiece.SetAnotherMoving(true);
            Boss.AttackReady(readyPosition);
            PlayAttackAnimation(handPiece);
            Boss.ReactPieces(handPiece, new Vector2(direction, 0.2f));

            sequence = DOTween.Sequence();
            sequence.Append(hand.DOMove(readyPosition, readyDuration).SetEase(Ease.OutBack));
            sequence.Join(hand.DORotate(new Vector3(0f, 0f, punchAngle), readyDuration).SetEase(Ease.OutCubic));
            yield return sequence.WaitForCompletion();

            DamageCaster?.EnableCasting(
                new DamageData(damage, DamageType.Melee),
                punchDuration + 0.08f
            );
            sequence = DOTween.Sequence();
            sequence.Append(hand.DOMove(punchPosition, punchDuration).SetEase(Ease.InQuart));
            sequence.AppendInterval(0.08f);
            yield return sequence.WaitForCompletion();
            Boss.AttackImpact(hand.position);
            DamageCaster?.DisableCasting();
            EndAttackAnimation();
            handPiece.SetAnotherMoving(false);
        }

        protected override void OnCancel()
        {
            sequence?.Kill();
            DamageCaster?.DisableCasting();
            if (Boss != null && Boss.RightHand != null && Boss.RightHand.IsAnotherMoving)
                Boss.RightHand.SetAnotherMoving(false);
        }
    }
}
