using System;
using System.Collections.Generic;
using Member.KYM.Scripts.Enemies.Boss.Splines.SplineEvents;
using UnityEngine;

namespace Member.KYM.Scripts.Enemies.Boss.Splines
{
    [Serializable]
    public enum MarkerType
    {
        
    }

    [Serializable]
    public class EventMarker
    {
        [Min(0)]
        public int knotIndex;

        public AbstractSplineEventDataSO[] events;
    }
    public class SplineMarkerManager : MonoBehaviour
    {
        [SerializeField] private EventMarker[] markers;

        private readonly HashSet<int> _executedMarkers = new();

        public void ResetMarkers(float startT)
        {
            _executedMarkers.Clear();

            // 진입 위치보다 뒤쪽에 있는 마커는 실행하지 않는다.
            // 필요하면 여기서 미리 executed 처리할 수 있음.
        }

        public void Process(SplinePath path, float previousT, float currentT, SplineEventContext context)
        {
            for (int i = 0; i < markers.Length; i++)
            {
                if (_executedMarkers.Contains(i))
                    continue;

                EventMarker marker = markers[i];
                float markerT = path.GetKnotT(marker.knotIndex);

                bool crossed =
                    previousT < markerT &&
                    currentT >= markerT;

                if (!crossed)
                    continue;

                _executedMarkers.Add(i);

                foreach (AbstractSplineEventDataSO splineEvent
                         in marker.events)
                {
                    splineEvent?.Handle(context);
                }
            }
        }
    }
}