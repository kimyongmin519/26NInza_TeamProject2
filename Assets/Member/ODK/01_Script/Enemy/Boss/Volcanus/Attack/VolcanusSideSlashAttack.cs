using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public class VolcanusSideSlashAttack : VolcanusSkill
    {
        [Header("Move / Slash")]
        [SerializeField] private float sideMoveDuration = 0.8f;
        [SerializeField] private float slashDistance = 18f;
        [SerializeField] private float slashHeight = 2f;
        [SerializeField] private float slashRotation = 720f;
        [SerializeField] private float slashDuration = 0.42f;
        [SerializeField] private float sawDamage = 70f;
        [SerializeField] private string sawAnimationStateName;
        [SerializeField] private float plusY = 4f;

        [Header("Fist Slam")]
        [SerializeField] private float readyHeight = 8f;
        [SerializeField] private float readyDuration = 0.42f;
        [SerializeField] private float slamDuration = 0.16f;
        [SerializeField] private float damage = 95f;
        [SerializeField] private DamageCaster fistDamageCaster;
        [SerializeField] private string fistAnimationStateName;

        private Sequence sequence;

        public override bool CanUseSkill(GameObject target = null)
        {
            return Boss != null && Boss.Saw != null && Boss.LeftHand != null && Boss.RightHand != null &&
                   Boss.LeftHand.IsUseable() && Boss.RightHand.IsUseable();
        }

        protected override IEnumerator Execute(GameObject target)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            float sideX = Boss.ArenaCenter.x + Boss.ArenaHalfWidth * side * 0.65f;
            Boss.Head?.React(null, new Vector2(-side * 0.75f, 0.35f), side * 14f);
            Boss.Truso?.React(null, new Vector2(-side * 0.45f, -0.2f), side * 8f);

            sequence = DOTween.Sequence();
            sequence.Append(Boss.BodyRoot.DOMoveX(sideX, sideMoveDuration).SetEase(Ease.InOutBack));
            sequence.Join(Boss.BodyRoot.DOLocalRotate(new Vector3(0f, 0f, -side * 9f), sideMoveDuration).SetEase(Ease.InOutSine));
            yield return sequence.WaitForCompletion();

            Boss.SetSawMoving(true);
            float slashY = Boss.GetGroundPoint(Boss.Target.position.x).y + slashHeight;
            Vector3 slashStart = new Vector3(Boss.ArenaCenter.x + side * Boss.ArenaHalfWidth, slashY + plusY, Boss.Saw.position.z);
            Vector3 slashEnd = slashStart + Vector3.left * side * slashDistance;
            Boss.AttackReady(slashStart);
            sequence = DOTween.Sequence();
            sequence.Append(Boss.Saw.DOMove(slashStart, 0.35f).SetEase(Ease.OutBack));
            sequence.Join(Boss.Saw.DOLocalRotate(new Vector3(0f, 0f, Boss.SawDownAngle), 0.35f).SetEase(Ease.OutCubic));
            yield return sequence.WaitForCompletion();

            PlayAttackAnimation(Boss.LeftHand, sawAnimationStateName);
            DamageCaster?.EnableCasting(
                new DamageData(sawDamage, DamageType.Melee),
                slashDuration
            );
            sequence = DOTween.Sequence();
            sequence.Append(Boss.Saw.DOMove(slashEnd, slashDuration).SetEase(Ease.InOutQuart));
            sequence.Join(Boss.Saw.DOLocalRotate(
                new Vector3(0f, 0f, Boss.SawDownAngle - side * slashRotation),
                slashDuration,
                RotateMode.FastBeyond360).SetEase(Ease.Linear));
            yield return sequence.WaitForCompletion();
            Boss.AttackImpact(slashEnd);
            DamageCaster?.DisableCasting();
            EndAttackAnimation();
            Boss.SetSawMoving(false);

            yield return FistSlam();

            sequence = DOTween.Sequence();
            sequence.Append(Boss.BodyRoot.DOLocalMove(Boss.BodyOriginLocalPosition, sideMoveDuration).SetEase(Ease.InOutSine));
            sequence.Join(Boss.BodyRoot.DOLocalRotate(Vector3.zero, sideMoveDuration).SetEase(Ease.InOutSine));
            yield return sequence.WaitForCompletion();
        }

        private IEnumerator FistSlam()
        {
            Transform fist = Boss.RightHand.transform;
            Vector3 groundPoint = Boss.GetGroundPoint(Boss.Target.position.x);
            Vector3 readyPosition = groundPoint + Vector3.up * readyHeight;
            Vector3 hitPosition = Boss.GetImpactVisualPosition(fist, groundPoint, Boss.FistImpactVisualOffset);
            Boss.RightHand.SetAnotherMoving(true);
            Boss.AttackReady(groundPoint);
            Boss.ReactPieces(Boss.RightHand, Vector2.down);
            sequence = DOTween.Sequence();
            sequence.Append(fist.DOLocalRotate(new Vector3(0f,0f,-32.329f),0.2f).SetEase(Ease.OutBack));
            sequence.Append(fist.DOMove(readyPosition, readyDuration).SetEase(Ease.OutBack));
            sequence.Join(fist.DORotate(new Vector3(0f, 0f, Boss.FistDownAngle), readyDuration).SetEase(Ease.OutQuad));
            yield return sequence.WaitForCompletion();

            PlayAttackAnimation(Boss.RightHand, fistAnimationStateName);
            fistDamageCaster?.EnableCasting(
                new DamageData(damage, DamageType.Melee),
                slamDuration + 0.08f
            );
            sequence = DOTween.Sequence();
            sequence.Append(fist.DOMove(hitPosition, slamDuration).SetEase(Ease.InExpo));
            sequence.AppendInterval(0.08f);
            yield return sequence.WaitForCompletion();
            Boss.AttackImpact(groundPoint);
            Boss.SpawnFistRocks(groundPoint);
            fistDamageCaster?.DisableCasting();
            EndAttackAnimation();
            Boss.RightHand.SetAnotherMoving(false);
        }

        protected override void OnCancel()
        {
            sequence?.Kill();
            DamageCaster?.DisableCasting();
            fistDamageCaster?.DisableCasting();
            if (Boss == null) return;
            Boss.SetSawMoving(false);
            if (Boss.RightHand != null && Boss.RightHand.IsAnotherMoving)
                Boss.RightHand.SetAnotherMoving(false);
        }
    }
}
