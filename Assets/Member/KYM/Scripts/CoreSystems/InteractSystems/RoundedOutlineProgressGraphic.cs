using UnityEngine;
using UnityEngine.UI;

namespace Member.KYM.Scripts.CoreSystems.InteractSystems
{
    public class RoundedOutlineProgressGraphic : MaskableGraphic
    {
        [SerializeField, Range(0f, 1f)] private float progress;
        [SerializeField] private float thickness = 5f;
        [SerializeField, Range(16, 128)] private int segments = 64;

        public float Progress
        {
            get => progress;
            set
            {
                progress = Mathf.Clamp01(value);
                SetVerticesDirty();
            }
        }

        public float Thickness
        {
            get => thickness;
            set
            {
                thickness = Mathf.Max(0f, value);
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (progress <= 0f)
                return;

            Rect rect = rectTransform.rect;
            float outerX = rect.width * 0.5f;
            float outerY = rect.height * 0.5f;
            float innerX = Mathf.Max(0f, outerX - thickness);
            float innerY = Mathf.Max(0f, outerY - thickness);
            int usedSegments = Mathf.Max(1, Mathf.CeilToInt(segments * progress));

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            for (int i = 0; i <= usedSegments; i++)
            {
                float normalized = Mathf.Min(
                    progress,
                    (float)i / segments
                );

                float angle = Mathf.PI * 0.5f - normalized * Mathf.PI * 2f;
                float cosine = Mathf.Cos(angle);
                float sine = Mathf.Sin(angle);
                Vector2 roundedDirection = new(
                    Mathf.Sign(cosine) * Mathf.Sqrt(Mathf.Abs(cosine)),
                    Mathf.Sign(sine) * Mathf.Sqrt(Mathf.Abs(sine))
                );

                vertex.position = new Vector3(
                    roundedDirection.x * outerX,
                    roundedDirection.y * outerY
                );
                vertexHelper.AddVert(vertex);

                vertex.position = new Vector3(
                    roundedDirection.x * innerX,
                    roundedDirection.y * innerY
                );
                vertexHelper.AddVert(vertex);
            }

            for (int i = 0; i < usedSegments; i++)
            {
                int index = i * 2;
                vertexHelper.AddTriangle(index, index + 2, index + 1);
                vertexHelper.AddTriangle(index + 2, index + 3, index + 1);
            }
        }
    }
}
