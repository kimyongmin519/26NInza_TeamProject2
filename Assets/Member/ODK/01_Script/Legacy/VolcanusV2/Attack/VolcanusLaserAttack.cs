using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus.Legacy
{
    public class VolcanusLaserAttack : VolcanusSkill
    {
        [Header("Aim")]
        [SerializeField] private float randomHorizontalRange = 6f;
        [SerializeField] private float laserOriginHeight = 8f;
        [SerializeField] private float moveDuration = 0.7f;
        [SerializeField] private float returnDuration = 0.65f;

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
        [SerializeField] private float damage = 24f;
        [SerializeField] private float damageInterval = 0.3f;
        [SerializeField] private LayerMask playerLayer = 1 << 6;
        [SerializeField] private Color warningColor = new Color(1f, 0.9f, 0.85f, 0.55f);
        [SerializeField] private Color fireColor = new Color(1f, 0.05f, 0.02f, 1f);
        [SerializeField] private Material laserMaterial;

        private readonly List<LineRenderer> lasers = new List<LineRenderer>();
        private Sequence sequence;
        private Tween widthTween;
        private bool ownsLaserMaterial;
        private float baseAngle;
        private float rotationDirection = 1f;
        private float nextDamageTime;
        private bool firing;
        private bool headMoved;

        public override bool CanUseSkill(GameObject target = null)
        {
            return Boss != null && Boss.Target != null && !Boss.IsDead;
        }

        protected override void OnVolcanusInitialize()
        {
            lasers.Clear();
            if (leftLaser != null) lasers.Add(leftLaser);
            if (rightLaser != null) lasers.Add(rightLaser);
            EnsureLaserCount(Mathf.Max(phaseOneBeamCount, phaseTwoBeamCount));
            foreach (LineRenderer line in lasers) ApplyLaserMaterial(line);
            SetVisibleCount(0);
        }

        protected override IEnumerator Execute(GameObject target)
        {
            bool phaseTwo = Boss.IsPhaseTwo;
            int beamCount = phaseTwo ? phaseTwoBeamCount : phaseOneBeamCount;
            float rotationSpeed = phaseTwo ? phaseTwoRotationSpeed : phaseOneRotationSpeed;
            EnsureLaserCount(beamCount);
            rotationDirection = Random.value < 0.5f ? -1f : 1f;
            baseAngle = startAngle + Random.Range(-startAngleRandom, startAngleRandom);
            firing = false;
            nextDamageTime = 0f;
            headMoved = false;

            Vector3 headOrigin = Vector3.zero;
            Quaternion headRotation = Quaternion.identity;
            bool useHead = !phaseTwo && Boss.Head != null && !Boss.Head.IsDestroyed;
            if (useHead)
            {
                Transform head = Boss.Head.transform;
                headOrigin = head.position;
                headRotation = head.rotation;
                float aimX = Mathf.Clamp(
                    Boss.Target.position.x + Random.Range(-randomHorizontalRange, randomHorizontalRange),
                    Boss.ArenaCenter.x - Boss.ArenaHalfWidth + 1f,
                    Boss.ArenaCenter.x + Boss.ArenaHalfWidth - 1f);
                Vector3 laserPosition = Boss.GetGroundPoint(aimX) + Vector3.up * laserOriginHeight;
                laserPosition.z = head.position.z;
                float moveDirection = Mathf.Sign(aimX - head.position.x);
                if (Mathf.Approximately(moveDirection, 0f)) moveDirection = 1f;

                Boss.Head.SetAnotherMoving(true);
                headMoved = true;
                Boss.Truso?.React(Boss.Head, new Vector2(-moveDirection * 0.55f, -0.3f), moveDirection * 9f);
                Boss.AttackReady(laserPosition);
                sequence = DOTween.Sequence();
                sequence.Append(head.DOMove(laserPosition, Scaled(moveDuration)).SetEase(Ease.InOutSine));
                sequence.Join(head.DORotate(Vector3.zero, Scaled(moveDuration)).SetEase(Ease.InOutSine));
                yield return sequence.WaitForCompletion();
            }
            else
            {
                Boss.AttackReady(Boss.LaserOrigin);
            }

            PlayAttackAnimation(useHead ? Boss.Head : Boss.Truso);
            SetVisibleCount(beamCount);
            float telegraph = Scaled(telegraphDuration);
            float elapsed = 0f;
            while (elapsed < telegraph && !Boss.IsDead && Boss.IsPhaseTwo == phaseTwo)
            {
                float blink = Mathf.PingPong(elapsed * 10f, 1f);
                Color color = warningColor;
                color.a *= Mathf.Lerp(0.35f, 1f, blink);
                SetStyle(color, warningWidth);
                UpdateLines(beamCount);
                elapsed += Time.deltaTime;
                yield return null;
            }

            float total = Scaled(duration);
            float onTime = Scaled(blinkOnDuration);
            float offTime = Scaled(blinkOffDuration);
            float flashTime = Scaled(blinkWarningFlash);
            bool on = true;
            float phaseTimer = 0f;
            elapsed = 0f;
            BeginFire();
            while (elapsed < total && !Boss.IsDead && Boss.IsPhaseTwo == phaseTwo)
            {
                float dt = Time.deltaTime;
                baseAngle += rotationDirection * rotationSpeed * Speed * dt;
                UpdateLines(beamCount);

                phaseTimer += dt;
                float limit = on ? onTime : offTime;
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
                else if (!on && limit - phaseTimer <= flashTime)
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
            EndAttackAnimation();
            Boss.AttackImpact(Boss.LaserOrigin);

            if (headMoved && !Boss.IsPhaseTwo && !Boss.IsDead && Boss.Head != null)
            {
                Transform head = Boss.Head.transform;
                sequence = DOTween.Sequence();
                sequence.Append(head.DOMove(headOrigin, Scaled(returnDuration)).SetEase(Ease.InOutSine));
                sequence.Join(head.DORotateQuaternion(headRotation, Scaled(returnDuration)).SetEase(Ease.InOutSine));
                yield return sequence.WaitForCompletion();
                Boss.Head.SetAnotherMoving(false);
            }
            headMoved = false;
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
                    SetStyle(Color.Lerp(fireColor, Color.white,
                        Mathf.InverseLerp(laserWidth, laserWidth * 1.8f, value)), value);
                }, laserWidth, Scaled(0.08f))
                .SetEase(Ease.OutQuad)
                .SetTarget(this);
            Boss.LaserFeedback(false);
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
            Vector3 origin = Boss.LaserOrigin;
            float radius = laserWidth * 0.5f;
            for (int i = 0; i < beamCount; i++)
            {
                RaycastHit2D hit = Physics2D.CircleCast(origin, radius, GetDirection(i, beamCount), laserLength, playerLayer);
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
            Vector3 start = Boss.LaserOrigin;
            for (int i = 0; i < lasers.Count && i < count; i++)
            {
                LineRenderer line = lasers[i];
                if (line == null) continue;
                line.SetPosition(0, start);
                line.SetPosition(1, start + (Vector3)GetDirection(i, count) * laserLength);
            }
        }

        private void EnsureLaserCount(int count)
        {
            while (lasers.Count < count)
            {
                GameObject laserObject = new GameObject($"Laser {lasers.Count}");
                laserObject.transform.SetParent(transform, false);
                LineRenderer line = laserObject.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.sortingOrder = leftLaser != null ? leftLaser.sortingOrder : 40;
                line.sortingLayerID = leftLaser != null ? leftLaser.sortingLayerID : 0;
                ApplyLaserMaterial(line);
                lasers.Add(line);
            }
        }

        private void ApplyLaserMaterial(LineRenderer line)
        {
            if (line == null) return;
            if (laserMaterial == null)
                laserMaterial = Resources.Load<Material>("ODKLaser");
            if (laserMaterial == null)
            {
                Shader shader = Shader.Find("ODK/Laser");
                if (shader != null)
                {
                    laserMaterial = new Material(shader);
                    ownsLaserMaterial = true;
                }
            }
            if (laserMaterial != null) line.sharedMaterial = laserMaterial;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.textureMode = LineTextureMode.Tile;
            line.numCapVertices = 8;
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
            sequence?.Kill();
            widthTween?.Kill();
            firing = false;
            SetVisibleCount(0);
            if (headMoved && Boss != null && Boss.Head != null && Boss.Head.IsAnotherMoving && !Boss.IsPhaseTwo)
                Boss.Head.SetAnotherMoving(false);
            headMoved = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            widthTween?.Kill();
            if (ownsLaserMaterial && laserMaterial != null) Destroy(laserMaterial);
        }
    }
}
