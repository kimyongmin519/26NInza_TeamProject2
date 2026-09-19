using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Bosses
{
    [DisallowMultipleComponent]
    public class BossArena : MonoBehaviour
    {
        [Header("Range")]
        [SerializeField] private Vector2 size = new Vector2(27f, 15f);
        [SerializeField] private Vector2 centerOffset;

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private Color gizmoColor = new Color(0.1f, 0.9f, 1f, 0.8f);

        public Vector3 Center => transform.position + (Vector3)centerOffset;
        public Vector2 Size => size;
        public Vector2 HalfSize => size * 0.5f;
        public float HalfWidth => size.x * 0.5f;
        public float HalfHeight => size.y * 0.5f;
        public float MinX => Center.x - HalfWidth;
        public float MaxX => Center.x + HalfWidth;
        public float MinY => Center.y - HalfHeight;
        public float MaxY => Center.y + HalfHeight;

        public Bounds Bounds => new Bounds(Center, new Vector3(size.x, size.y, 0f));

        public Vector3 Clamp(Vector3 position, float padding = 0f)
        {
            float safePadding = Mathf.Max(0f, padding);
            float minX = Mathf.Min(Center.x, MinX + safePadding);
            float maxX = Mathf.Max(Center.x, MaxX - safePadding);
            float minY = Mathf.Min(Center.y, MinY + safePadding);
            float maxY = Mathf.Max(Center.y, MaxY - safePadding);

            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.y = Mathf.Clamp(position.y, minY, maxY);
            return position;
        }

        public bool Contains(Vector3 position, float padding = 0f)
        {
            float safePadding = Mathf.Max(0f, padding);
            return position.x >= MinX + safePadding &&
                   position.x <= MaxX - safePadding &&
                   position.y >= MinY + safePadding &&
                   position.y <= MaxY - safePadding;
        }

        private void OnValidate()
        {
            size.x = Mathf.Max(0.1f, size.x);
            size.y = Mathf.Max(0.1f, size.y);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(Center, new Vector3(size.x, size.y, 0f));
            Gizmos.DrawLine(Center + Vector3.left * 0.35f, Center + Vector3.right * 0.35f);
            Gizmos.DrawLine(Center + Vector3.down * 0.35f, Center + Vector3.up * 0.35f);
        }
    }
}
