using System.Collections;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus.Legacy
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
        [SerializeField, Range(0.2f, 1.2f)] private float sawRadiusRatio = 0.65f;

        [Header("Fist Slam")]
        [SerializeField] private float readyHeight = 8f;
        [SerializeField] private float readyDuration = 0.42f;
        [SerializeField] private float slamDuration = 0.16f;
        [SerializeField] private float damage = 95f;
        [SerializeField] private DamageCaster fistDamageCaster;
        [SerializeField] private string fistAnimationStateName;
        [SerializeField, Range(0.2f, 1.2f)] private float fistRadiusRatio = 0.65f;

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
            sequence.Append(Boss.BodyRoot.DOMoveX(sideX, Scaled(sideMoveDuration)).SetEase(Ease.InOutSine));
            sequence.Join(Boss.BodyRoot.DOLocalRotate(new Vector3(0f, 0f, -side * 9f), Scaled(sideMoveDuration)).SetEase(Ease.InOutSine));
            yield return sequence.WaitForCompletion();

            Boss.SetSawMoving(true);
            float slashY = Boss.GetGroundPoint(Boss.Target.position.x).y + slashHeight;
            Vector3 slashStart = new Vector3(Boss.ArenaCenter.x + side * Boss.ArenaHalfWidth, slashY + plusY, Boss.Saw.position.z);
            Vector3 slashEnd = slashStart + Vector3.left * side * slashDistance;
            Boss.AttackReady(slashStart);
            sequence = DOTween.Sequence();
            sequence.Append(Boss.Saw.DOMove(slashStart, Scaled(0.35f)).SetEase(Ease.OutBack));
            sequence.Join(Boss.Saw.DOLocalRotate(new Vector3(0f, 0f, Boss.SawDownAngle), Scaled(0.35f)).SetEase(Ease.OutCubic));
            yield return sequence.WaitForCompletion();

            PlayAttackAnimation(Boss.LeftHand, sawAnimationStateName);
            StartCoroutine(StrikeWindow(DamageCaster, Volcanus.StrikePart.Saw, Scaled(slashDuration), sawDamage, DamageType.Melee, sawRadiusRatio));
            sequence = DOTween.Sequence();
            sequence.Append(Boss.Saw.DOMove(slashEnd, Scaled(slashDuration)).SetEase(Ease.InOutQuart));
            sequence.Join(Boss.Saw.DOLocalRotate(
                new Vector3(0f, 0f, Boss.SawDownAngle - side * slashRotation),
                Scaled(slashDuration),
                RotateMode.FastBeyond360).SetEase(Ease.Linear));
            yield return sequence.WaitForCompletion();
            Boss.AttackImpact(slashEnd);
            DamageCaster?.DisableCasting();
            EndAttackAnimation();
            Boss.SetSawMoving(false);

            yield return FistSlam();

            sequence = DOTween.Sequence();
            sequence.Append(Boss.BodyRoot.DOLocalMove(Boss.BodyOriginLocalPosition, Scaled(sideMoveDuration)).SetEase(Ease.InOutSine));
            sequence.Join(Boss.BodyRoot.DOLocalRotate(Vector3.zero, Scaled(sideMoveDuration)).SetEase(Ease.InOutSine));
            yield return sequence.WaitForCompletion();
        }

        private IEnumerator FistSlam()
        {
            Transform fist = Boss.RightHand.transform;
            Vector3 groundPoint = Boss.GetGroundPoint(Boss.Target.position.x);
            Vector3 readyPosition = groundPoint + Vector3.up * readyHeight;
            readyPosition.z = fist.position.z;
            Boss.RightHand.SetAnotherMoving(true);
            Boss.AttackReady(groundPoint);
            Boss.ReactPieces(Boss.RightHand, Vector2.down);
            sequence = DOTween.Sequence();
            sequence.Append(fist.DOMove(readyPosition, Scaled(readyDuration)).SetEase(Ease.OutBack));
            sequence.Join(fist.DORotate(new Vector3(0f, 0f, Boss.FistDownAngle), Scaled(readyDuration)).SetEase(Ease.OutQuad));
            yield return sequence.WaitForCompletion();

            Vector3 hitPosition = Boss.GetImpactVisualPosition(fist, groundPoint, Boss.FistImpactVisualOffset);
            DamageCaster fistCaster = fistDamageCaster != null ? fistDamageCaster : DamageCaster;
            PlayAttackAnimation(Boss.RightHand, fistAnimationStateName);
            StartCoroutine(StrikeWindow(fistCaster, Volcanus.StrikePart.Fist,
                Scaled(slamDuration) + 0.08f, damage, DamageType.Melee, fistRadiusRatio));
            sequence = DOTween.Sequence();
            sequence.Append(fist.DOMove(hitPosition, Scaled(slamDuration)).SetEase(Ease.InExpo));
            sequence.AppendInterval(0.08f);
            yield return sequence.WaitForCompletion();
            Boss.AttackImpact(groundPoint);
            Boss.SpawnFistRocks(groundPoint);
            fistCaster?.DisableCasting();
            EndAttackAnimation();
            Boss.RightHand.SetAnotherMoving(false);
        }

        protected override void OnVolcanusCancel()
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
