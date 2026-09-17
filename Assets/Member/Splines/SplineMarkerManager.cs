using System;
using System.Collections.Generic;
using Member.KYM.Scripts.Enemies.Boss.Splines.SplineEvents;
using Reflex.Core;
using UnityEngine;

namespace Member.KYM.Scripts.Enemies.Boss.Splines
{
    [Serializable]
    public class EventMarker
    {
        [Min(0)]
        public int knotIndex;

        public AbstractSplineEventDataSO[] events;
    }
    public class SplineMarkerManager : MonoBehaviour, IInstaller
    {
        [SerializeField] private EventMarker[] markers;

        private readonly HashSet<int> _executedMarkers = new();

        public void ResetMarkers()
        {
            _executedMarkers.Clear();
        }

        public void ProcessAt(
            SplinePath path,
            float currentT,
            SplineEventContext context)
        {
            if (markers == null)
                return;

            for (int i = 0; i < markers.Length; i++)
            {
                if (_executedMarkers.Contains(i))
                    continue;

                float markerT = path.GetKnotT(markers[i].knotIndex);
                if (!Mathf.Approximately(markerT, currentT))
                    continue;

                ExecuteMarker(i, context);
            }
        }

        public void Process(SplinePath path, float previousT, float currentT, SplineEventContext context)
        {
            if (markers == null)
                return;

            for (int i = 0; i < markers.Length; i++)
            {
                if (_executedMarkers.Contains(i))
                    continue;

                EventMarker marker = markers[i];
                float markerT = path.GetKnotT(marker.knotIndex);

                bool crossedForward =
                    previousT < markerT &&
                    currentT >= markerT;

                bool crossedBackward =
                    previousT > markerT &&
                    currentT <= markerT;

                if (!crossedForward && !crossedBackward)
                    continue;

                ExecuteMarker(i, context);
            }
        }

        private void ExecuteMarker(int markerIndex, SplineEventContext context)
        {
            _executedMarkers.Add(markerIndex);

            EventMarker marker = markers[markerIndex];
            if (marker.events == null)
                return;

            SplineEventContext markerContext =
                context.WithKnotIndex(marker.knotIndex);

            foreach (AbstractSplineEventDataSO splineEvent in marker.events)
            {
                splineEvent?.Handle(markerContext);
            }
        }

        public void InstallBindings(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterValue(this);
        }
    }
}
