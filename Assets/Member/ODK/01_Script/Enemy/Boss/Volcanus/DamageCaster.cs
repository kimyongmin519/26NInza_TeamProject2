using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "DamageCastMode")]
    public enum DamageCastMode
    {
        Circle,
        Box,
        Capsule,
        OutsideBox
    }

    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "DamageCastingMode")]
    public enum DamageCastingMode
    {
        Instant,
        Timed,
        Manual
    }

    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "DamageCaster")]
    public class DamageCaster : MonoBehaviour
    {
        [Header("Cast Setting")]
        [SerializeField] private DamageCastMode castMode;
        [SerializeField] private Transform referenceTransform;
        [SerializeField] private Vector2 positionOffset;
        [SerializeField] private bool rotateOffsetWithReference;
        [SerializeField] private Vector2 size = Vector2.one;
        [SerializeField] private float range = 1f;
        [SerializeField] private float angle;
        [SerializeField] private CapsuleDirection2D capsuleDirection = CapsuleDirection2D.Horizontal;
        [SerializeField] private bool useReferenceRotation;
        [SerializeField] private LayerMask targetLayer = 1 << 6;

        [Header("Casting Setting")]
        [SerializeField] private DamageCastingMode castingMode;
        [SerializeField] private float castingDuration = 0.2f;

        [Header("Debug Mode")]
        [SerializeField] private bool debugMode = true;
        [SerializeField] private Color debugColor = new Color(0.35f, 1f, 0.1f, 0.9f);
        [SerializeField] private float debugRemainTime = 1.5f;

        private readonly List<DebugCast> debugCasts = new List<DebugCast>();
        private Material runtimeDebugMaterial;
        private bool hasWorldPositionOverride;
        private bool hasWorldAngleOverride;
        private Vector3 worldPositionOverride;
        private float worldAngleOverride;
        private readonly HashSet<IDamageable> instantDamagedTargets = new HashSet<IDamageable>();
        private readonly HashSet<HealthModule> instantDamagedHealthModules = new HashSet<HealthModule>();
        private readonly HashSet<IDamageable> castingDamagedTargets = new HashSet<IDamageable>();
        private readonly HashSet<HealthModule> castingDamagedHealthModules = new HashSet<HealthModule>();
        private DamageData castingDamage;
        private float castingEndTime;
        private bool hasCastingDamage;

        public bool IsCasting { get; private set; }

        private Transform ReferenceTransform => referenceTransform != null ? referenceTransform : transform;
        private Vector3 CastCenter
        {
            get
            {
                if (hasWorldPositionOverride) return worldPositionOverride;
                Transform basis = ReferenceTransform;
                Vector2 offset = rotateOffsetWithReference
                    ? basis.TransformDirection(positionOffset)
                    : positionOffset;
                return basis.position + (Vector3)offset;
            }
        }
        private float CastAngle => hasWorldAngleOverride
            ? worldAngleOverride
            : angle + (useReferenceRotation ? ReferenceTransform.eulerAngles.z : 0f);

        public bool Cast(DamageData damage)
        {
            instantDamagedTargets.Clear();
            instantDamagedHealthModules.Clear();
            return Cast(damage, instantDamagedTargets, instantDamagedHealthModules);
        }

        public void SetDamage(DamageData damage)
        {
            castingDamage = damage;
            hasCastingDamage = true;
        }

        public void EnableCasting(DamageData damage)
        {
            SetDamage(damage);
            EnableCasting();
        }

        public void EnableCasting(DamageData damage, float duration)
        {
            SetDamage(damage);
            EnableCasting(duration);
        }

        public void EnableCasting()
        {
            if (!hasCastingDamage) return;
            if (castingMode == DamageCastingMode.Instant)
            {
                Cast(castingDamage);
                return;
            }

            BeginCasting();
            castingEndTime = castingMode == DamageCastingMode.Timed
                ? Time.time + castingDuration
                : float.PositiveInfinity;
        }

        public void EnableCasting(float duration)
        {
            if (!hasCastingDamage) return;
            BeginCasting();
            castingEndTime = Time.time + Mathf.Max(Time.fixedDeltaTime, duration);
        }

        public void DisableCasting()
        {
            IsCasting = false;
            castingDamagedTargets.Clear();
            castingDamagedHealthModules.Clear();
        }

        private void BeginCasting()
        {
            IsCasting = true;
            castingDamagedTargets.Clear();
            castingDamagedHealthModules.Clear();
            Cast(castingDamage, castingDamagedTargets, castingDamagedHealthModules);
        }

        private void FixedUpdate()
        {
            if (!IsCasting) return;
            if (Time.time >= castingEndTime)
            {
                DisableCasting();
                return;
            }

            Cast(castingDamage, castingDamagedTargets, castingDamagedHealthModules);
        }

        private bool Cast(DamageData damage, HashSet<IDamageable> damagedTargets, HashSet<HealthModule> damagedHealthModules)
        {
            Vector3 center = CastCenter;
            float castAngle = CastAngle;
            Collider2D[] hits = GetHits(center, castAngle);
            bool applied = false;

            foreach (Collider2D hit in hits)
            {
                if (hit == null || castMode == DamageCastMode.OutsideBox && IsInsideBox(hit.bounds.center, center, castAngle))
                    continue;

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null) damageable = hit.GetComponentInChildren<IDamageable>();
                if (damageable != null)
                {
                    if (!damagedTargets.Add(damageable)) continue;
                    damageable.TakeDamage(damage);
                    applied = true;
                    continue;
                }

                HealthModule health = hit.GetComponentInParent<HealthModule>();
                if (health == null) health = hit.GetComponentInChildren<HealthModule>();
                if (health == null || !damagedHealthModules.Add(health)) continue;
                health.ApplyDamage(damage);
                applied = true;
            }

            AddDebugCast(center, castAngle, applied);
            return applied;
        }

        public void SetWorldPosition(Vector3 position)
        {
            hasWorldPositionOverride = true;
            worldPositionOverride = position;
        }

        public void SetWorldPose(Vector3 position, float worldAngle)
        {
            SetWorldPosition(position);
            hasWorldAngleOverride = true;
            worldAngleOverride = worldAngle;
        }

        public void SetRange(float value) => range = Mathf.Max(0f, value);
        public void SetSize(Vector2 value) => size = new Vector2(Mathf.Abs(value.x), Mathf.Abs(value.y));

        public static bool ApplyDamage(Transform target, DamageData damage)
        {
            if (target == null) return false;

            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable == null) damageable = target.GetComponentInChildren<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                return true;
            }

            HealthModule health = target.GetComponentInParent<HealthModule>();
            if (health == null) health = target.GetComponentInChildren<HealthModule>();
            if (health == null) return false;
            health.ApplyDamage(damage);
            return true;
        }

        private Collider2D[] GetHits(Vector3 center, float castAngle)
        {
            return castMode switch
            {
                DamageCastMode.Circle => Physics2D.OverlapCircleAll(center, range, targetLayer),
                DamageCastMode.Box => Physics2D.OverlapBoxAll(center, size, castAngle, targetLayer),
                DamageCastMode.Capsule => Physics2D.OverlapCapsuleAll(center, size, capsuleDirection, castAngle, targetLayer),
                DamageCastMode.OutsideBox => Physics2D.OverlapCircleAll(center, range, targetLayer),
                _ => System.Array.Empty<Collider2D>()
            };
        }

        private bool IsInsideBox(Vector3 point, Vector3 center, float castAngle)
        {
            Vector2 localPoint = Quaternion.Euler(0f, 0f, -castAngle) * (point - center);
            return Mathf.Abs(localPoint.x) <= size.x * 0.5f && Mathf.Abs(localPoint.y) <= size.y * 0.5f;
        }

        private void AddDebugCast(Vector3 center, float castAngle, bool applied)
        {
            if (!debugMode) return;
            debugCasts.Add(new DebugCast(castMode, center, size, range, castAngle, capsuleDirection, applied, Time.realtimeSinceStartup + debugRemainTime));
            if (debugCasts.Count > 32) debugCasts.RemoveAt(0);
        }

        private void OnDrawGizmosSelected()
        {
            if (!debugMode) return;

            CleanupDebugCasts();
            //if (!Application.isPlaying)
            //{
                Gizmos.color = debugColor;
                DrawGizmoShape(castMode, CastCenter, size, range, CastAngle, capsuleDirection);
            //}
            foreach (DebugCast cast in debugCasts)
            {
                Gizmos.color = GetCastColor(cast.applied);
                DrawGizmoShape(cast.mode, cast.center, cast.size, cast.range, cast.angle, cast.capsuleDirection);
            }
        }

        private void OnRenderObject()
        {
            if (!Application.isPlaying || !debugMode) return;

            CleanupDebugCasts();
            if (debugCasts.Count == 0 || !CreateRuntimeDebugMaterial()) return;

            runtimeDebugMaterial.SetPass(0);
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.LINES);
            foreach (DebugCast cast in debugCasts)
            {
                GL.Color(GetCastColor(cast.applied));
                DrawRuntimeShape(cast.mode, cast.center, cast.size, cast.range, cast.angle, cast.capsuleDirection);
            }
            GL.End();
            GL.PopMatrix();
        }

        private void DrawGizmoShape(DamageCastMode mode, Vector3 center, Vector2 castSize, float castRange, float castAngle, CapsuleDirection2D direction)
        {
            if (mode == DamageCastMode.Circle)
            {
                Gizmos.DrawWireSphere(center, castRange);
                return;
            }
            if (mode == DamageCastMode.OutsideBox)
            {
                Gizmos.DrawWireSphere(center, castRange);
                DrawGizmoBox(center, castSize, castAngle);
                return;
            }
            if (mode == DamageCastMode.Box)
            {
                DrawGizmoBox(center, castSize, castAngle);
                return;
            }

            GetCapsulePoints(center, castSize, castAngle, direction, out Vector3 start, out Vector3 end, out float radius);
            DrawGizmoCapsule(start, end, radius);
        }

        private void DrawGizmoBox(Vector3 center, Vector2 castSize, float castAngle)
        {
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(0f, 0f, castAngle), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, castSize);
            Gizmos.matrix = previous;
        }

        private void DrawGizmoCapsule(Vector3 start, Vector3 end, float radius)
        {
            Vector2 direction = ((Vector2)end - (Vector2)start).normalized;
            Vector3 normal = new Vector3(-direction.y, direction.x) * radius;
            Gizmos.DrawLine(start + normal, end + normal);
            Gizmos.DrawLine(start - normal, end - normal);
            Gizmos.DrawWireSphere(start, radius);
            Gizmos.DrawWireSphere(end, radius);
        }

        private void DrawRuntimeShape(DamageCastMode mode, Vector3 center, Vector2 castSize, float castRange, float castAngle, CapsuleDirection2D direction)
        {
            if (mode == DamageCastMode.Circle)
            {
                DrawRuntimeCircle(center, castRange);
                return;
            }
            if (mode == DamageCastMode.OutsideBox)
            {
                DrawRuntimeCircle(center, castRange);
                DrawRuntimeBox(center, castSize, castAngle);
                return;
            }
            if (mode == DamageCastMode.Box)
            {
                DrawRuntimeBox(center, castSize, castAngle);
                return;
            }

            GetCapsulePoints(center, castSize, castAngle, direction, out Vector3 start, out Vector3 end, out float radius);
            DrawRuntimeCapsule(start, end, radius);
        }

        private void GetCapsulePoints(Vector3 center, Vector2 castSize, float castAngle, CapsuleDirection2D direction, out Vector3 start, out Vector3 end, out float radius)
        {
            bool horizontal = direction == CapsuleDirection2D.Horizontal;
            float length = horizontal ? castSize.x : castSize.y;
            float diameter = horizontal ? castSize.y : castSize.x;
            radius = diameter * 0.5f;
            float halfLine = Mathf.Max(0f, length * 0.5f - radius);
            Vector3 axis = Quaternion.Euler(0f, 0f, castAngle) * (horizontal ? Vector3.right : Vector3.up);
            start = center - axis * halfLine;
            end = center + axis * halfLine;
        }

        private void DrawRuntimeBox(Vector3 center, Vector2 castSize, float castAngle)
        {
            Quaternion rotation = Quaternion.Euler(0f, 0f, castAngle);
            Vector3 half = castSize * 0.5f;
            Vector3 a = center + rotation * new Vector3(-half.x, -half.y);
            Vector3 b = center + rotation * new Vector3(-half.x, half.y);
            Vector3 c = center + rotation * new Vector3(half.x, half.y);
            Vector3 d = center + rotation * new Vector3(half.x, -half.y);
            DrawRuntimeLine(a, b);
            DrawRuntimeLine(b, c);
            DrawRuntimeLine(c, d);
            DrawRuntimeLine(d, a);
        }

        private void DrawRuntimeCapsule(Vector3 start, Vector3 end, float radius)
        {
            if (Vector2.Distance(start, end) <= Mathf.Epsilon)
            {
                DrawRuntimeCircle(start, radius);
                return;
            }
            Vector2 direction = ((Vector2)end - (Vector2)start).normalized;
            Vector3 normal = new Vector3(-direction.y, direction.x) * radius;
            DrawRuntimeLine(start + normal, end + normal);
            DrawRuntimeLine(start - normal, end - normal);
            DrawRuntimeCircle(start, radius);
            DrawRuntimeCircle(end, radius);
        }

        private void DrawRuntimeCircle(Vector3 center, float radius)
        {
            const int segmentCount = 32;
            Vector3 previous = center + Vector3.right * radius;
            for (int i = 1; i <= segmentCount; i++)
            {
                float circleAngle = Mathf.PI * 2f * i / segmentCount;
                Vector3 current = center + new Vector3(Mathf.Cos(circleAngle), Mathf.Sin(circleAngle)) * radius;
                DrawRuntimeLine(previous, current);
                previous = current;
            }
        }

        private void DrawRuntimeLine(Vector3 start, Vector3 end)
        {
            GL.Vertex(start);
            GL.Vertex(end);
        }

        private bool CreateRuntimeDebugMaterial()
        {
            if (runtimeDebugMaterial != null) return true;
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null) return false;

            runtimeDebugMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            runtimeDebugMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            runtimeDebugMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            runtimeDebugMaterial.SetInt("_Cull", (int)CullMode.Off);
            runtimeDebugMaterial.SetInt("_ZWrite", 0);
            runtimeDebugMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
            return true;
        }

        private void CleanupDebugCasts()
        {
            if (!Application.isPlaying) return;
            float currentTime = Time.realtimeSinceStartup;
            for (int i = debugCasts.Count - 1; i >= 0; i--)
            {
                if (debugCasts[i].endTime < currentTime)
                    debugCasts.RemoveAt(i);
            }
        }

        private Color GetCastColor(bool applied)
        {
            Color color = debugColor;
            color.a = applied ? debugColor.a : debugColor.a * 0.45f;
            return color;
        }

        private readonly struct DebugCast
        {
            public readonly DamageCastMode mode;
            public readonly Vector3 center;
            public readonly Vector2 size;
            public readonly float range;
            public readonly float angle;
            public readonly CapsuleDirection2D capsuleDirection;
            public readonly bool applied;
            public readonly float endTime;

            public DebugCast(DamageCastMode mode, Vector3 center, Vector2 size, float range, float angle, CapsuleDirection2D capsuleDirection, bool applied, float endTime)
            {
                this.mode = mode;
                this.center = center;
                this.size = size;
                this.range = range;
                this.angle = angle;
                this.capsuleDirection = capsuleDirection;
                this.applied = applied;
                this.endTime = endTime;
            }
        }


        private void OnDisable() => DisableCasting();

        private void OnDestroy()
        {
            if (runtimeDebugMaterial != null)
                Destroy(runtimeDebugMaterial);
        }
    }
}
