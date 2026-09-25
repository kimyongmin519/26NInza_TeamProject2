using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    public class MoonOrbitBeamAttack : MoonSkill
    {
        [Header("Orbit")]
        [SerializeField] private float arenaPadding = 1.5f;
        [SerializeField] private float entryDuration = 0.55f;
        [SerializeField] private float legDuration = 1.15f;
        [SerializeField] private float returnDuration = 0.5f;
        [SerializeField] private float travelFloatAmplitude = 0.55f;
        [SerializeField] private float travelFloatCycles = 1.5f;

        [Header("Light Shot")]
        [SerializeField] private float shotInterval = 0.34f;
        [SerializeField] private float randomAngle = 9f;
        [SerializeField] private float beamWarningDuration = 0.22f;
        [SerializeField] private float beamActiveDuration = 0.16f;
        [SerializeField] private float beamWidth = 0.32f;
        [SerializeField] private float beamDamage = 30f;
        [SerializeField] private Color beamColor = new Color(0.55f, 0.8f, 1f, 1f);

        [Header("Phase Two Clone")]
        [SerializeField] private float cloneInterval = 0.85f;
        [SerializeField] private float cloneSpeed = 8f;
        [SerializeField] private float cloneDamage = 32f;

        private Vector3 originPosition;
        private bool hasOrigin;

        public override bool CanUseSkill(GameObject target = null)
        {
            return Boss != null && Boss.Target != null && !Boss.IsDead;
        }

        protected override IEnumerator ExecuteMoon(GameObject target)
        {
            originPosition = Boss.transform.position;
            hasOrigin = true;
            Vector3 center = Boss.ArenaCenter;
            float left = center.x - Boss.ArenaHalfWidth + arenaPadding;
            float right = center.x + Boss.ArenaHalfWidth - arenaPadding;
            float top = center.y + Boss.ArenaHalfHeight - arenaPadding + 3;
            float bottom = center.y - Boss.ArenaHalfHeight + arenaPadding - 3;
            float z = Boss.transform.position.z;
            Vector3[] corners =
            {
                new Vector3(left, top, z),
                new Vector3(right, top, z),
                new Vector3(right, bottom, z),
                new Vector3(left, bottom, z),
                new Vector3(left, top, z)
            };

            Boss.AttackReady(corners[0]);
            Boss.transform.DOKill();
            yield return Boss.transform.DOMove(corners[0], entryDuration / DurationScale)
                .SetEase(Ease.InOutSine)
                .WaitForCompletion();

            float cloneTimer = cloneInterval;
            for (int i = 1; i < corners.Length && !Boss.IsDead; i++)
            {
                float elapsed = 0f;
                float shotTimer = 0f;
                Vector3 start = Boss.transform.position;
                Vector3 end = corners[i];
                while (elapsed < legDuration)
                {
                    float delta = Time.deltaTime * DurationScale;
                    elapsed += delta;
                    float rate = Mathf.Clamp01(elapsed / Mathf.Max(0.02f, legDuration));
                    Vector3 position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, rate));
                    position.y += Mathf.Sin(rate * Mathf.PI * 2f * travelFloatCycles + i) *
                                  travelFloatAmplitude;
                    Boss.transform.position = position;

                    shotTimer -= delta;
                    if (shotTimer <= 0f)
                    {
                        FireLight();
                        shotTimer = shotInterval;
                    }

                    if (Boss.IsPhaseTwo)
                    {
                        cloneTimer -= delta;
                        if (cloneTimer <= 0f)
                        {
                            ThrowClone();
                            cloneTimer = cloneInterval;
                        }
                    }
                    yield return null;
                }
                Boss.transform.position = end;
            }

            yield return Boss.transform.DOMove(originPosition, returnDuration / DurationScale)
                .SetEase(Ease.InOutSine)
                .WaitForCompletion();
            hasOrigin = false;
        }

        private void FireLight()
        {
            if (Boss.Target == null) return;
            Vector2 direction = Boss.Target.position - Boss.transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg +
                Random.Range(-randomAngle, randomAngle);
            direction = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );
            float length = Mathf.Sqrt(
                Boss.ArenaHalfWidth * Boss.ArenaHalfWidth +
                Boss.ArenaHalfHeight * Boss.ArenaHalfHeight
            ) * 2.5f;
            Boss.SpawnLaser(
                Boss.transform.position,
                direction,
                length,
                beamWidth,
                beamWarningDuration / DurationScale,
                beamActiveDuration / DurationScale,
                beamDamage,
                beamColor
            );
        }

        private void ThrowClone()
        {
            if (Boss.Target == null) return;
            SpriteRenderer source = Boss.GetComponentInChildren<SpriteRenderer>();
            Vector2 direction = (Boss.Target.position - Boss.transform.position).normalized;
            MoonHazardProjectile clone = Boss.SpawnHazard(
                Boss.transform.position,
                direction * cloneSpeed,
                MoonHazardProjectile.MoveMode.Homing,
                Boss.Target,
                cloneDamage,
                5f,
                source != null ? source.sprite : null,
                new Color(0.65f, 0.78f, 1f, 0.32f)
            );
            if (source != null && clone != null)
                clone.transform.localScale = source.transform.lossyScale * 0.55f;
            Boss.PlayCloneFeedback(Boss.transform.position);
        }

        protected override void OnMoonCancel()
        {
            if (Boss == null) return;
            Boss.transform.DOKill();
            if (hasOrigin) Boss.transform.position = originPosition;
            hasOrigin = false;
        }
    }
}
