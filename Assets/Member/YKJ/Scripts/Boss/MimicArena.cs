using System;
using UnityEngine;

namespace Member.YKJ.Bosses
{
    public sealed class MimicArena : MonoBehaviour
    {
        [Serializable]
        public sealed class Zone
        {
            [Tooltip("Y and Z define the landing position. X is calculated from the equal zone center.")]
            public Transform LandingPoint;
            [Tooltip("Rock spawn height/depth. X is calculated from the equal zone bounds.")]
            public Transform RockLeft;
            public Transform RockRight;
            public bool IsConfigured => LandingPoint != null;
        }

        [SerializeField] private Zone[] zones = { new Zone(), new Zone(), new Zone() };
        [Header("Three Equal Zones (relative to arena position)")]
        [Tooltip("X = left edge, Y = right edge. This entire width is divided into three equal parts.")]
        [SerializeField] private Vector2 horizontalRange = new Vector2(-12.444445f, 12.444445f);
        [Tooltip("X = bottom, Y = top of both the warning and the landing damage box.")]
        [SerializeField] private Vector2 warningVerticalRange = new Vector2(-2f, 8f);
        public int ZoneCount => zones?.Length ?? 0;
        public bool IsConfigured => ZoneCount == 3 && horizontalRange.y > horizontalRange.x &&
            warningVerticalRange.y > warningVerticalRange.x &&
            Array.TrueForAll(zones, zone => zone != null && zone.IsConfigured);

        public static Rect DivideZone(Vector2 horizontal, Vector2 vertical, int index)
        {
            if (index < 0 || index >= 3) throw new ArgumentOutOfRangeException(nameof(index));
            float left = Mathf.Lerp(horizontal.x, horizontal.y, index / 3f);
            float right = Mathf.Lerp(horizontal.x, horizontal.y, (index + 1) / 3f);
            return Rect.MinMaxRect(left, vertical.x, right, vertical.y);
        }

        public Rect ZoneBounds(int index)
        {
            Rect bounds = DivideZone(horizontalRange, warningVerticalRange, index);
            bounds.position += (Vector2)transform.position;
            return bounds;
        }

        public Vector3 LandingPosition(int index)
        {
            Vector3 position = zones[index].LandingPoint.position;
            position.x = ZoneBounds(index).center.x;
            return position;
        }

        public Vector3 RandomRockPosition(int index)
        {
            float t = UnityEngine.Random.value;
            Vector3 position = Vector3.Lerp(zones[index].RockLeft.position, zones[index].RockRight.position, t);
            Rect bounds = ZoneBounds(index);
            position.x = Mathf.Lerp(bounds.xMin, bounds.xMax, t);
            return position;
        }

        private void OnDrawGizmosSelected()
        {
            if (!IsConfigured)
                return;
            for (int i = 0; i < ZoneCount; i++)
            {
                Rect bounds = ZoneBounds(i);
                Gizmos.color = new Color(1f, 0.35f, 0.35f, 0.15f);
                Vector3 center = new Vector3(bounds.center.x, bounds.center.y, transform.position.z);
                Gizmos.DrawCube(center, new Vector3(bounds.width, bounds.height, 0.01f));
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(center, new Vector3(bounds.width, bounds.height, 0.01f));
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(LandingPosition(i), 0.3f);
            }
        }
    }
}
