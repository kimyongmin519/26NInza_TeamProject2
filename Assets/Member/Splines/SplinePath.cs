using Reflex.Attributes;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Member.KYM.Scripts.Enemies.Boss.Splines
{
    [RequireComponent(typeof(SplineContainer))]
    public class SplinePath : MonoBehaviour
    {
        [field: SerializeField] public int SplineIndex { get; private set; }

        [Inject][field: SerializeField] public SplineMarkerManager MarkerManager { get; private set; }

        public SplineContainer Container { get; private set; }

        public Spline Spline =>
            Container.Splines[SplineIndex];

        public float Length =>
            Container.CalculateLength(SplineIndex);

        private void Awake()
        {
            Container = GetComponent<SplineContainer>();
        }

        // 보스와 가장 가까운 스플라인 위치를 찾는다.
        public float GetNearestPoint(
            Vector3 worldPosition,
            out Vector3 nearestWorldPosition)
        {
            using var nativeSpline = new NativeSpline(
                Spline,
                Container.transform.localToWorldMatrix);

            float distance = SplineUtility.GetNearestPoint(
                nativeSpline,
                worldPosition,
                out float3 nearest,
                out float normalizedT);

            nearestWorldPosition = nearest;
            return normalizedT;
        }

        public Vector2 EvaluatePosition(float normalizedT)
        {
            return Container.EvaluatePosition(
                SplineIndex,
                normalizedT).xy;
        }

        // 첫 번째와 마지막 Knot 중 현재 위치에서 가까운 쪽의 T를 반환한다.
        public float GetClosestEndT(Vector2 worldPosition)
        {
            Vector2 firstKnotPosition = EvaluatePosition(0f);
            Vector2 lastKnotPosition = EvaluatePosition(1f);

            float firstSqrDistance = (worldPosition - firstKnotPosition).sqrMagnitude;
            float lastSqrDistance = (worldPosition - lastKnotPosition).sqrMagnitude;

            return firstSqrDistance <= lastSqrDistance ? 0f : 1f;
        }

        public float GetKnotT(int knotIndex)
        {
            return Spline.ConvertIndexUnit(
                knotIndex,
                PathIndexUnit.Knot,
                PathIndexUnit.Normalized);
        }

        private void OnDrawGizmosSelected()
        {
            if (!TryGetComponent(out SplineContainer container))
                return;

            if (SplineIndex < 0 || SplineIndex >= container.Splines.Count)
                return;

            Gizmos.color = Color.yellow;
            Spline spline = container.Splines[SplineIndex];

            for (int i = 0; i < spline.Count; i++)
            {
                BezierKnot knot = spline[i];
                Vector3 position = new(
                    knot.Position.x,
                    knot.Position.y,
                    knot.Position.z);
                Vector3 worldPosition = container.transform.TransformPoint(position);

                Gizmos.DrawSphere(worldPosition, 0.15f);
#if UNITY_EDITOR
                Handles.Label(worldPosition + Vector3.up * 0.25f, $"Knot {i}");
#endif
            }
        }
    }
}
