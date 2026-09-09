using Reflex.Attributes;
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

        public float GetKnotT(int knotIndex)
        {
            return Spline.ConvertIndexUnit(
                knotIndex,
                PathIndexUnit.Knot,
                PathIndexUnit.Normalized);
        }
    }
}