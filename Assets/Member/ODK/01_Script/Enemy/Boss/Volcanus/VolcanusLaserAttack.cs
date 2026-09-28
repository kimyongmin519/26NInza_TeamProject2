using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public class VolcanusLaserAttack : VolcanusSkill
    {
        [Header("Beam Count")]
        [SerializeField, Min(1)] private int phaseOneBeamCount = 2;
        [SerializeField, Min(1)] private int phaseTwoBeamCount = 3;

        [Header("Timing")]
        [SerializeField] private float telegraphDuration = 0.9f;
        [SerializeField] private float duration = 4f;
        [SerializeField] private float blinkOnDuration = 0.55f;
        [SerializeField] private float blinkOffDuration = 0.28f;
        [SerializeField] private float blinkWarningFlash = 0.08f;

        [Header("Rotation")]
        [SerializeField] private float phaseOneRotationSpeed = 38f;
        [SerializeField] private float phaseTwoRotationSpeed = 52f;
        [SerializeField] private float startAngle = -90f;
        [SerializeField] private float startAngleRandom = 25f;
        [SerializeField] private bool reverseEachBlink;

        [Header("Laser")]
        [SerializeField] private LineRenderer leftLaser;
        [SerializeField] private LineRenderer rightLaser;
        [SerializeField] private float laserLength = 40f;
        [SerializeField] private float laserWidth = 0.45f;
        [SerializeField] private float warningWidth = 0.08f;
        [SerializeField] private float damage = 1f;
        [SerializeField] private float damageInterval = 0.3f;
        [SerializeField] private Color warningColor = new Color(1f, 0.9f, 0.85f, 0.55f);
        [SerializeField] private Color fireColor = new Color(1f, 0.08f, 0.02f, 1f);
        [SerializeField] private Material laserMaterial;

        private readonly List<LineRenderer> lasers = new List<LineRenderer>();
        private float baseAngle;
        private float rotationDirection = 1f;
        private float nextDamageTime;
        private bool firing;
        private Tween widthTween;

        protected override string DefaultActionState => Volcanus.RoarState;

        public override bool CanUseSkill(GameObject target = null) =>
            Boss != null && Boss.Target != null && !Boss.IsDead;

        protected override void OnVolcanusInitialize()
        {
            lasers.Clear();
            if (leftLaser == null) leftLaser = CreateLaser("Laser 0");
            if (rightLaser == null) rightLaser = CreateLaser("Laser 1");
            lasers.Add(leftLaser);
            lasers.Add(rightLaser);
            EnsureLaserCount(Mathf.Max(phaseOneBeamCount, phaseTwoBeamCount));
            SetVisibleCount(0);
        }

        protected override IEnumerator ExecuteVolcanus(GameObject target)
        {
            int beamCount = Boss.IsPhaseTwo ? phaseTwoBeamCount : phaseOneBeamCount;
            float rotationSpeed = Boss.IsPhaseTwo ? phaseTwoRotationSpeed : phaseOneRotationSpeed;
            EnsureLaserCount(beamCount);
            rotationDirection = Random.value < 0.5f ? -1f : 1f;
            baseAngle = startAngle + Random.Range(-startAngleRandom, startAngleRandom);
            firing = false;
            nextDamageTime = 0f;

            float telegraph = telegraphDuration / ActionSpeed;
            float total = duration / ActionSpeed;
            Vector3 groundPoint = Boss.GetGroundPoint(Boss.transform.position.x);
            Boss.AttackReady(Boss.HeadPosition);
            Boss.PlayFeedback(VolcanusFeedbackType.Ready, Boss.HeadPosition);
            Boss.PlayAction(ActionState, ActionSpeed);
            Boss.AnimateLaser(groundPoint, telegraph, total);

            SetVisibleCount(beamCount);
            SetStyle(warningColor, warningWidth);
            float elapsed = 0f;
            while (elapsed < telegraph && !Boss.IsDead)
            {
                float blink = Mathf.PingPong(elapsed * 10f, 1f);
                SetStyle(new Color(warningColor.r, warningColor.g, warningColor.b, warningColor.a * Mathf.Lerp(0.35f, 1f, blink)), warningWidth);
                UpdateLines(beamCount);
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;
            bool on = true;
            float phaseTimer = 0f;
            BeginFire();
            while (elapsed < total && !Boss.IsDead)
            {
                float dt = Time.deltaTime;
                baseAngle += rotationDirection * rotationSpeed * ActionSpeed * dt;
                UpdateLines(beamCount);

                phaseTimer += dt;
                float limit = (on ? blinkOnDuration : blinkOffDuration) / ActionSpeed;
                if (phaseTimer >= limit)
                {
                    phaseTimer = 0f;
                    on = !on;
                    if (on)
                    {
                        if (reverseEachBlink) rotationDirection *= -1f;
                        BeginFire();
                    }
                    else EndFire();
                }
                else if (!on && limit - phaseTimer <= blinkWarningFlash / ActionSpeed)
                {
                    SetStyle(Color.white, warningWidth * 2.2f);
                }

                if (firing && Time.time >= nextDamageTime && TryHitPlayer(beamCount))
                    nextDamageTime = Time.time + damageInterval;

                elapsed += dt;
                yield return null;
            }

            EndFire();
            SetVisibleCount(0);
            Boss.AttackImpact(groundPoint);
            Boss.Shake(false);
        }

        private void BeginFire()
        {
            firing = true;
            widthTween?.Kill();
            float width = laserWidth * 1.8f;
            SetStyle(Color.white, width);
            widthTween = DOTween.To(() => width, value =>
                {
                    width = value;
                    SetStyle(Color.Lerp(fireColor, Color.white, Mathf.InverseLerp(laserWidth, laserWidth * 1.8f, value)), value);
                }, laserWidth, 0.08f / ActionSpeed)
                .SetEase(Ease.OutQuad)
                .SetTarget(this);
            Boss.PlayFeedback(VolcanusFeedbackType.Impact, Boss.HeadPosition);
            Boss.ShakeCamera(0.35f);
        }

        private void EndFire()
        {
            firing = false;
            widthTween?.Kill();
            SetStyle(warningColor, warningWidth);
        }

        private bool TryHitPlayer(int beamCount)
        {
            Vector3 origin = Boss.HeadPosition;
            float radius = laserWidth * 0.5f * Mathf.Clamp(DamageCaster.PlayerHitScale, 0.05f, 1f);
            for (int i = 0; i < beamCount; i++)
            {
                Vector2 direction = GetDirection(i, beamCount);
                RaycastHit2D hit = Physics2D.CircleCast(origin, radius, direction, laserLength, Boss.PlayerLayer);
                if (hit.collider == null) continue;
                DamageCaster.ApplyDamage(hit.collider.transform, new DamageData(damage, DamageType.Beam));
                return true;
            }
            return false;
        }

        private Vector2 GetDirection(int index, int count)
        {
            float angle = baseAngle + 360f / Mathf.Max(1, count) * index;
            float radians = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        private void UpdateLines(int count)
        {
            Vector3 start = Boss.HeadPosition;
            for (int i = 0; i < lasers.Count; i++)
            {
                LineRenderer line = lasers[i];
                if (line == null || i >= count) continue;
                Vector2 direction = GetDirection(i, count);
                line.SetPosition(0, start);
                line.SetPosition(1, start + (Vector3)direction * laserLength);
            }
        }

        private void EnsureLaserCount(int count)
        {
            while (lasers.Count < count)
                lasers.Add(CreateLaser($"Laser {lasers.Count}"));
        }

        private LineRenderer CreateLaser(string objectName)
        {
            GameObject laserObject = new GameObject(objectName);
            laserObject.transform.SetParent(transform, false);
            LineRenderer line = laserObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.numCapVertices = 8;
            line.sortingOrder = 40;
            line.sharedMaterial = laserMaterial != null
                ? laserMaterial
                : Resources.Load<Material>("ODKLaser");
            return line;
        }

        private void SetStyle(Color color, float width)
        {
            foreach (LineRenderer line in lasers)
            {
                if (line == null || !line.gameObject.activeSelf) continue;
                line.startColor = color;
                line.endColor = color;
                line.startWidth = width;
                line.endWidth = width;
            }
        }

        private void SetVisibleCount(int count)
        {
            for (int i = 0; i < lasers.Count; i++)
                if (lasers[i] != null) lasers[i].gameObject.SetActive(i < count);
        }

        protected override void OnVolcanusCancel()
        {
            widthTween?.Kill();
            firing = false;
            SetVisibleCount(0);
        }
    }
}
