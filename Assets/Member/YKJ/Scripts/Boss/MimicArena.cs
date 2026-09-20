using System;
using UnityEngine;

namespace Member.YKJ.Bosses
{
    public sealed class MimicArena : MonoBehaviour
    {
        [Serializable]
        public sealed class Zone
        {
            public Transform LandingPoint;
            public Transform RockLeft;
            public Transform RockRight;
            public bool IsConfigured => LandingPoint != null && RockLeft != null && RockRight != null;
        }

        [SerializeField] private Zone[] zones = { new Zone(), new Zone(), new Zone() };
        public int ZoneCount => zones?.Length ?? 0;
        public bool IsConfigured => ZoneCount == 3 && Array.TrueForAll(zones, zone => zone != null && zone.IsConfigured);
        public Vector3 LandingPosition(int index) => zones[index].LandingPoint.position;
        public Vector3 RandomRockPosition(int index) =>
            Vector3.Lerp(zones[index].RockLeft.position, zones[index].RockRight.position, UnityEngine.Random.value);

        private void OnDrawGizmosSelected()
        {
            if (zones == null)
                return;
            foreach (Zone zone in zones)
            {
                if (zone == null || !zone.IsConfigured)
                    continue;
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(zone.LandingPoint.position, 0.3f);
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(zone.RockLeft.position, zone.RockRight.position);
            }
        }
    }
}
