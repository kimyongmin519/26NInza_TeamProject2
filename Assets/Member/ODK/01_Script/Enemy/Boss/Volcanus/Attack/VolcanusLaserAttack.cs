using System.Collections;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public class VolcanusLaserAttack : VolcanusSkill
    {
        [Header("Aim")]
        [SerializeField] private float randomHorizontalRange = 6f;
        [SerializeField] private float laserOriginHeight = 8f;
        [SerializeField] private float moveDuration = 0.7f;
        [SerializeField] private float minimumReadyDuration = 0.55f;
        [SerializeField] private float maximumReadyDuration = 1.35f;
        [SerializeField] private float safeHalfWidth = 1.35f;
        [SerializeField] private float safeHeight = 2.5f;

        [Header("Laser")]
        [SerializeField] private LineRenderer leftLaser;
        [SerializeField] private LineRenderer rightLaser;
        [SerializeField] private float duration = 2.2f;
        [SerializeField] private float laserLength = 40f;
        [SerializeField] private float laserWidth = 0.45f;
        [SerializeField, Range(0f, 80f)] private float startSpreadAngle = 35f;
        [SerializeField] private float damage = 24f;
        [SerializeField] private float damageInterval = 0.3f;
        [SerializeField] private float returnDuration = 0.65f;

        private Sequence sequence;
        private Material laserMaterial;
        private Vector3 aimGroundPoint;
        private float currentReadyDuration;
        private float laserSweepProgress = 1f;
        private Tween laserSweepTween;

        public override bool CanUseSkill(GameObject target = null)
        {
            return Boss != null && Boss.Head != null && Boss.Target != null && !Boss.IsPhaseTwo;
        }

        protected override void OnVolcanusInitialize()
        {
            if (leftLaser == null) leftLaser = CreateLaser("Left Laser");
            if (rightLaser == null) rightLaser = CreateLaser("Right Laser");
            SetLaserActive(false);
        }

        protected override IEnumerator Execute(GameObject target)
        {
            Transform head = Boss.Head.transform;
            Vector3 originPosition = head.position;
            Quaternion originRotation = head.rotation;
            float playerX = Boss.Target.position.x;
            float randomOffset = Random.Range(-randomHorizontalRange, randomHorizontalRange);
            float aimX = Mathf.Clamp(
                playerX + randomOffset,
                Boss.ArenaCenter.x - Boss.ArenaHalfWidth + safeHalfWidth,
                Boss.ArenaCenter.x + Boss.ArenaHalfWidth - safeHalfWidth
            );

            aimGroundPoint = Boss.GetGroundPoint(aimX);
            Vector3 laserPosition = aimGroundPoint + Vector3.up * laserOriginHeight;
            laserPosition.z = head.position.z;
            float moveDistance = Mathf.Abs(aimX - playerX);
            float distanceRate = randomHorizontalRange <= Mathf.Epsilon
                ? 0f
                : Mathf.Clamp01(moveDistance / randomHorizontalRange);
            currentReadyDuration = Mathf.Lerp(maximumReadyDuration, minimumReadyDuration, distanceRate);
            float moveDirection = Mathf.Sign(aimX - head.position.x);
            if (Mathf.Approximately(moveDirection, 0f)) moveDirection = 1f;

            Boss.Head.SetAnotherMoving(true);
            Boss.Truso?.React(Boss.Head, new Vector2(-moveDirection * 0.55f, -0.3f), moveDirection * 9f);
            Boss.LeftHand?.React(Boss.Head, new Vector2(-moveDirection * 0.35f, -0.2f), moveDirection * 6f);
            Boss.RightHand?.React(Boss.Head, new Vector2(-moveDirection * 0.35f, -0.2f), moveDirection * 6f);
            Boss.AttackReady(aimGroundPoint);

            sequence = DOTween.Sequence();
            sequence.Append(head.DOMove(laserPosition + Vector3.up * 0.5f, moveDuration * 0.72f).SetEase(Ease.InOutBack));
            sequence.Join(head.DORotate(new Vector3(0f, 0f, -moveDirection * 11f), moveDuration * 0.72f).SetEase(Ease.InOutSine));
            sequence.Append(head.DOMove(laserPosition, moveDuration * 0.28f).SetEase(Ease.OutBounce));
            sequence.Join(head.DORotate(Vector3.zero, moveDuration * 0.28f).SetEase(Ease.OutBack));
            yield return sequence.WaitForCompletion();

            SetLaserActive(true);
            SetLaserColor(new Color(1f, 0.05f, 0.02f, 1f));
            laserSweepProgress = 0f;
            UpdateLaserLines();
            laserSweepTween = DOTween.To(
                    () => laserSweepProgress,
                    value =>
                    {
                        laserSweepProgress = value;
                        UpdateLaserLines();
                    },
                    1f,
                    currentReadyDuration
                )
                .SetEase(Ease.InOutSine);
            yield return laserSweepTween.WaitForCompletion();
            laserSweepTween = null;
            if (Boss.IsPhaseTwo || Boss.IsDead) yield break;

            PlayAttackAnimation(Boss.Head);
            Boss.LaserFeedback(false);
            float damageTime = 0f;
            float currentTime = 0f;
            while (currentTime < duration && !Boss.IsPhaseTwo && !Boss.IsDead)
            {
                UpdateLaserLines();
                damageTime -= Time.deltaTime;
                if (damageTime <= 0f)
                {
                    DamageOutsideSafeZone();
                    damageTime = damageInterval;
                }
                currentTime += Time.deltaTime;
                yield return null;
            }

            SetLaserActive(false);
            EndAttackAnimation();
            if (Boss.IsPhaseTwo || Boss.IsDead) yield break;

            sequence = DOTween.Sequence();
            sequence.Append(head.DOMove(originPosition, returnDuration).SetEase(Ease.InOutBack));
            sequence.Join(head.DORotateQuaternion(originRotation, returnDuration).SetEase(Ease.InOutSine));
            if (Boss.Truso != null)
                sequence.Join(Boss.Truso.transform.DOPunchPosition(Vector3.down * 0.25f, returnDuration, 5, 0.45f));
            yield return sequence.WaitForCompletion();
            Boss.Head.SetAnotherMoving(false);
        }

        private LineRenderer CreateLaser(string laserName)
        {
            GameObject laserObject = new GameObject(laserName);
            laserObject.transform.SetParent(transform);
            LineRenderer line = laserObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.startWidth = laserWidth;
            line.endWidth = laserWidth;
            line.useWorldSpace = true;
            line.sortingOrder = 20;
            Shader shader = Shader.Find("Sprites/Default");
            if (laserMaterial == null && shader != null)
                laserMaterial = new Material(shader);
            if (laserMaterial != null) line.sharedMaterial = laserMaterial;
            return line;
        }

        private void SetLaserActive(bool active)
        {
            if (leftLaser != null) leftLaser.gameObject.SetActive(active);
            if (rightLaser != null) rightLaser.gameObject.SetActive(active);
        }

        private void SetLaserColor(Color color)
        {
            if (leftLaser != null)
            {
                leftLaser.startColor = color;
                leftLaser.endColor = color;
                leftLaser.startWidth = laserWidth;
                leftLaser.endWidth = laserWidth;
            }
            if (rightLaser != null)
            {
                rightLaser.startColor = color;
                rightLaser.endColor = color;
                rightLaser.startWidth = laserWidth;
                rightLaser.endWidth = laserWidth;
            }
        }

        private void UpdateLaserLines()
        {
            if (Boss.Head == null) return;
            Vector3 start = Boss.Head.transform.position;
            Vector3 leftEnd = aimGroundPoint + Vector3.left * safeHalfWidth;
            Vector3 rightEnd = aimGroundPoint + Vector3.right * safeHalfWidth;
            Vector3 startLeftDirection = Quaternion.Euler(0f, 0f, startSpreadAngle) * Vector3.up;
            Vector3 startRightDirection = Quaternion.Euler(0f, 0f, -startSpreadAngle) * Vector3.up;
            Vector3 leftDirection = Vector3.Slerp(
                startLeftDirection,
                (leftEnd - start).normalized,
                laserSweepProgress
            ).normalized;
            Vector3 rightDirection = Vector3.Slerp(
                startRightDirection,
                (rightEnd - start).normalized,
                laserSweepProgress
            ).normalized;
            SetLaserLine(leftLaser, start, leftDirection);
            SetLaserLine(rightLaser, start, rightDirection);
        }

        private void SetLaserLine(LineRenderer line, Vector3 start, Vector3 direction)
        {
            if (line == null) return;
            line.SetPosition(0, start);
            line.SetPosition(1, start + direction * laserLength);
        }

        private void DamageOutsideSafeZone()
        {
            Vector3 safeCenter = aimGroundPoint + Vector3.up * (safeHeight - 0.5f) * 0.5f;
            DamageCaster?.SetWorldPosition(safeCenter);
            CastDamage(damage, DamageType.Beam);
        }

        private void OnDrawGizmosSelected()
        {
            Volcanus boss = Boss != null ? Boss : GetComponentInParent<Volcanus>();
            if (boss == null || boss.Head == null) return;

            Vector3 groundPoint = Application.isPlaying && aimGroundPoint != Vector3.zero
                ? aimGroundPoint
                : boss.GetGroundPoint(boss.Target != null ? boss.Target.position.x : boss.ArenaCenter.x);
            Vector3 start = groundPoint + Vector3.up * laserOriginHeight;
            Vector3 leftEnd = groundPoint + Vector3.left * safeHalfWidth;
            Vector3 rightEnd = groundPoint + Vector3.right * safeHalfWidth;
            Vector3 startLeftDirection = Quaternion.Euler(0f, 0f, startSpreadAngle) * Vector3.up;
            Vector3 startRightDirection = Quaternion.Euler(0f, 0f, -startSpreadAngle) * Vector3.up;

            Gizmos.color = new Color(1f, 0.65f, 0.1f, 0.7f);
            Gizmos.DrawLine(start, start + startLeftDirection * laserLength);
            Gizmos.DrawLine(start, start + startRightDirection * laserLength);
            Gizmos.color = new Color(1f, 0.15f, 0f, 0.85f);
            Gizmos.DrawLine(start, start + (leftEnd - start).normalized * laserLength);
            Gizmos.DrawLine(start, start + (rightEnd - start).normalized * laserLength);
            Gizmos.color = new Color(0.1f, 1f, 0.35f, 0.85f);
            Gizmos.DrawWireCube(
                groundPoint + Vector3.up * (safeHeight - 0.5f) * 0.5f,
                new Vector3(safeHalfWidth * 2f, safeHeight + 0.5f, 0f)
            );
            Gizmos.DrawLine(start, groundPoint);
        }

        protected override void OnVolcanusCancel()
        {
            sequence?.Kill();
            laserSweepTween?.Kill();
            laserSweepTween = null;
            SetLaserActive(false);
            if (Boss != null && Boss.Head != null && Boss.Head.IsAnotherMoving)
                Boss.Head.SetAnotherMoving(false);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (laserMaterial != null) Destroy(laserMaterial);
        }
    }
}
