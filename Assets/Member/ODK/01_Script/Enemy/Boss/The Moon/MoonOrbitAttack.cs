using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Member.ODK.Scripts.Enemys.Combat;
using UnityEngine;

namespace Member.ODK.Scripts.Enemys.MoonBoss
{
    public class MoonOrbitAttack : MoonSkill
    {
        [Header("Warning / Movement")]
        [SerializeField] private LineRenderer directionLine;
        [SerializeField] private MoonTelegraphLine directionTelegraph;
        [SerializeField] private float warningDuration = 1f;
        [SerializeField] private float phaseOneDuration = 4.5f;
        [SerializeField] private float phaseTwoDuration = 5.5f;
        [SerializeField] private float moveSpeed = 17f;
        [SerializeField] private float collisionRadius = 1.5f;
        [SerializeField] private float returnDuration = 0.55f;
        [SerializeField] private float contactDamage = 45f;

        [Header("Wall Shockwave")]
        [SerializeField] private DamageCaster wallShockwaveCaster;
        [SerializeField] private float wallShockwaveRadius = 2.2f;
        [SerializeField] private float wallShockwaveDamage = 22f;

        [Header("Phase Two Fragment")]
        [SerializeField] private MoonFragment fragmentPrefab;
        [SerializeField] private float rotationSpeed = 360f;
        [SerializeField] private float fragmentAngleStep = 45f;
        [SerializeField] private float fragmentSpeed = 12f;
        [SerializeField] private float fragmentDamage = 28f;
        [SerializeField] private float fragmentLifeTime = 5f;

        private readonly List<DamageCaster> contactCasters = new List<DamageCaster>();
        private readonly List<OriginState> originStates = new List<OriginState>();
        private float fragmentRotation;

        public override bool CanUseSkill(GameObject target = null)
        {
            return Boss != null && Boss.Target != null && !Boss.IsDead;
        }

        protected override void OnMoonInitialize()
        {
            if (directionLine == null)
                directionLine = CreateLine("Orbit Direction", 0.1f);
            directionTelegraph = directionLine.GetComponent<MoonTelegraphLine>();
            if (directionTelegraph == null)
                directionTelegraph = directionLine.gameObject.AddComponent<MoonTelegraphLine>();
            directionTelegraph.Hide();

            if (wallShockwaveCaster == null)
                wallShockwaveCaster = CreateCaster("Orbit Wall Shockwave");

            EnsureContactCasters(2);
        }

        protected override IEnumerator ExecuteMoon(GameObject target)
        {
            BuildOriginStates();
            ShowDirection(originStates.Count > 0
                ? originStates[0].Velocity
                : Vector2.right);
            Boss.AttackReady(Boss.transform.position);
            yield return new WaitForSeconds(warningDuration / DurationScale);
            HideDirection();

            float duration = Boss.IsPhaseTwo ? phaseTwoDuration : phaseOneDuration;
            EnableContactDamage(duration / DurationScale);
            fragmentRotation = 0f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float deltaTime = Time.deltaTime * DurationScale;
                MoveAndBounce(deltaTime);
                RotateAndFireFragments(deltaTime);
                UpdateContactCasterPositions();
                elapsed += deltaTime;
                yield return null;
            }

            DisableContactDamage();
            yield return ReturnOrigins();
            Boss.AttackImpact(Boss.transform.position);
        }

        private void BuildOriginStates()
        {
            originStates.Clear();
            Vector2 baseDirection = GetRandomDirection();
            int index = 0;
            foreach (Transform origin in Boss.GetPatternOrigins())
            {
                originStates.Add(new OriginState(
                    origin,
                    origin.position,
                    origin.rotation,
                    index == 0 ? baseDirection : -baseDirection
                ));
                index++;
            }
            EnsureContactCasters(originStates.Count);
        }

        private void MoveAndBounce(float deltaTime)
        {
            Vector3 center = Boss.ArenaCenter;
            float minX = center.x - Boss.ArenaHalfWidth + collisionRadius;
            float maxX = center.x + Boss.ArenaHalfWidth - collisionRadius;
            float minY = center.y - Boss.ArenaHalfHeight + collisionRadius;
            float maxY = center.y + Boss.ArenaHalfHeight - collisionRadius;

            for (int i = 0; i < originStates.Count; i++)
            {
                OriginState state = originStates[i];
                if (state.Transform == null) continue;

                Vector3 position = state.Transform.position +
                    (Vector3)(state.Velocity * moveSpeed * deltaTime);
                bool bounced = false;

                if (position.x < minX || position.x > maxX)
                {
                    position.x = Mathf.Clamp(position.x, minX, maxX);
                    state.Velocity.x *= -1f;
                    bounced = true;
                }
                if (position.y < minY || position.y > maxY)
                {
                    position.y = Mathf.Clamp(position.y, minY, maxY);
                    state.Velocity.y *= -1f;
                    bounced = true;
                }

                state.Transform.position = position;
                if (bounced) CastWallShockwave(position);
                originStates[i] = state;
            }
        }

        private void RotateAndFireFragments(float deltaTime)
        {
            float rotationDelta = rotationSpeed * deltaTime;
            foreach (OriginState state in originStates)
            {
                if (state.Transform != null)
                    state.Transform.Rotate(0f, 0f, rotationDelta);
            }

            if (!Boss.IsPhaseTwo) return;
            fragmentRotation += Mathf.Abs(rotationDelta);
            float step = Mathf.Max(1f, fragmentAngleStep);
            while (fragmentRotation >= step)
            {
                fragmentRotation -= step;
                foreach (OriginState state in originStates)
                {
                    if (state.Transform != null)
                        SpawnFragment(state.Transform);
                }
            }
        }

        private void SpawnFragment(Transform origin)
        {
            Vector2 direction = origin.right;
            MoonFragment fragment = fragmentPrefab != null
                ? Instantiate(fragmentPrefab, origin.position, origin.rotation)
                : CreateFallbackFragment(origin.position, origin.rotation);
            fragment.Launch(direction, fragmentSpeed, fragmentDamage, fragmentLifeTime);
        }

        private MoonFragment CreateFallbackFragment(Vector3 position, Quaternion rotation)
        {
            GameObject fragmentObject = new GameObject("Moon Fragment");
            fragmentObject.transform.SetPositionAndRotation(position, rotation);
            Rigidbody2D body = fragmentObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            CircleCollider2D collider = fragmentObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.18f;

            TrailRenderer trail = fragmentObject.AddComponent<TrailRenderer>();
            trail.time = 0.25f;
            trail.startWidth = 0.16f;
            trail.endWidth = 0f;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) trail.material = new Material(shader);
            return fragmentObject.AddComponent<MoonFragment>();
        }

        private void CastWallShockwave(Vector3 position)
        {
            if (wallShockwaveCaster != null)
            {
                wallShockwaveCaster.ConfigureCircle(wallShockwaveRadius, Boss.PlayerLayer);
                wallShockwaveCaster.SetWorldPosition(position);
                wallShockwaveCaster.Cast(new DamageData(wallShockwaveDamage, DamageType.Special));
            }

            Boss.PlayShockwave(
                position,
                wallShockwaveRadius,
                new Color(0.65f, 0.85f, 1f, 0.8f)
            );
            Boss.AttackImpact(position);
        }

        private void EnableContactDamage(float duration)
        {
            for (int i = 0; i < originStates.Count; i++)
            {
                DamageCaster caster = contactCasters[i];
                caster.ConfigureCircle(collisionRadius, Boss.PlayerLayer);
                caster.SetWorldPosition(originStates[i].Transform.position);
                caster.EnableCasting(
                    new DamageData(contactDamage, DamageType.Melee),
                    duration
                );
            }
        }

        private void UpdateContactCasterPositions()
        {
            for (int i = 0; i < originStates.Count && i < contactCasters.Count; i++)
            {
                if (originStates[i].Transform != null)
                    contactCasters[i].SetWorldPosition(originStates[i].Transform.position);
            }
        }

        private void DisableContactDamage()
        {
            foreach (DamageCaster caster in contactCasters)
                caster?.DisableCasting();
        }

        private IEnumerator ReturnOrigins()
        {
            foreach (OriginState state in originStates)
            {
                if (state.Transform == null) continue;
                state.Transform.DOKill();
                state.Transform.DOMove(state.StartPosition, returnDuration / DurationScale)
                    .SetEase(Ease.InOutSine);
                state.Transform.DORotateQuaternion(
                    state.StartRotation,
                    returnDuration / DurationScale
                )
                    .SetEase(Ease.InOutSine);
            }
            yield return new WaitForSeconds(returnDuration / DurationScale);
        }

        private void ShowDirection(Vector2 direction)
        {
            if (directionLine == null) return;
            Vector3 start = Boss.transform.position;
            Vector3 end = start + (Vector3)direction * 5f;
            if (directionTelegraph != null)
                directionTelegraph.Show(start, end, warningDuration * 0.75f / DurationScale);
            else
            {
                directionLine.positionCount = 2;
                directionLine.SetPosition(0, start);
                directionLine.SetPosition(1, end);
                directionLine.enabled = true;
            }
        }

        private void HideDirection()
        {
            if (directionTelegraph != null) directionTelegraph.Hide();
            else if (directionLine != null) directionLine.enabled = false;
        }

        private static Vector2 GetRandomDirection()
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            if (Mathf.Abs(direction.x) < 0.35f)
                direction.x = direction.x < 0f ? -0.35f : 0.35f;
            if (Mathf.Abs(direction.y) < 0.35f)
                direction.y = direction.y < 0f ? -0.35f : 0.35f;
            return direction.normalized;
        }

        private void EnsureContactCasters(int count)
        {
            while (contactCasters.Count < count)
                contactCasters.Add(CreateCaster($"Orbit Contact {contactCasters.Count + 1}"));
        }

        private DamageCaster CreateCaster(string casterName)
        {
            GameObject casterObject = new GameObject(casterName);
            casterObject.transform.SetParent(transform, false);
            return casterObject.AddComponent<DamageCaster>();
        }

        private LineRenderer CreateLine(string lineName, float width)
        {
            GameObject lineObject = new GameObject(lineName);
            lineObject.transform.SetParent(transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.widthMultiplier = width;
            line.startColor = new Color(0.65f, 0.85f, 1f, 0.85f);
            line.endColor = new Color(0.65f, 0.85f, 1f, 0.1f);
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) line.material = new Material(shader);
            return line;
        }

        protected override void OnCancel()
        {
            HideDirection();
            DisableContactDamage();
            foreach (OriginState state in originStates)
            {
                if (state.Transform == null) continue;
                state.Transform.DOKill();
                state.Transform.SetPositionAndRotation(state.StartPosition, state.StartRotation);
            }
            originStates.Clear();
        }

        private struct OriginState
        {
            public Transform Transform;
            public Vector3 StartPosition;
            public Quaternion StartRotation;
            public Vector2 Velocity;

            public OriginState(
                Transform transform,
                Vector3 startPosition,
                Quaternion startRotation,
                Vector2 velocity)
            {
                Transform = transform;
                StartPosition = startPosition;
                StartRotation = startRotation;
                Velocity = velocity;
            }
        }
    }
}
