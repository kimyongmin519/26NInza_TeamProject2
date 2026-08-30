using UnityEngine;
using UnityEngine.UI;

namespace Member.KYM.Scripts.CoreSystems.InteractSystems
{
    public class RoundedRectGraphic : MaskableGraphic
    {
        [SerializeField] private float radius = 16f;
        [SerializeField, Range(2, 16)] private int cornerSegments = 6;

        public float Radius
        {
            get => radius;
            set
            {
                radius = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect rect = rectTransform.rect;
            float usedRadius = Mathf.Min(
                Mathf.Max(0f, radius),
                Mathf.Min(rect.width, rect.height) * 0.5f
            );

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = rect.center;
            vertexHelper.AddVert(vertex);

            int perimeterCount = cornerSegments * 4;
            for (int i = 0; i <= perimeterCount; i++)
            {
                float normalized = (float)i / perimeterCount;
                float angle = normalized * Mathf.PI * 2f;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 corner = new(
                    direction.x >= 0f
                        ? rect.xMax - usedRadius
                        : rect.xMin + usedRadius,
                    direction.y >= 0f
                        ? rect.yMax - usedRadius
                        : rect.yMin + usedRadius
                );

                vertex.position = corner + direction * usedRadius;
                vertexHelper.AddVert(vertex);
            }

            for (int i = 1; i <= perimeterCount; i++)
                vertexHelper.AddTriangle(0, i, i + 1);
        }
    }
}
