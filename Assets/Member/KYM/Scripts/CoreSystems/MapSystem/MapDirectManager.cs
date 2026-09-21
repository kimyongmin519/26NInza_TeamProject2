using System.Collections.Generic;
using UnityEngine;

namespace Member.KYM.Scripts.CoreSystems.MapSystem
{
    public class MapDirectManager : MonoBehaviour
    {
        private readonly Dictionary<int, List<IMapDirectTarget>> _targetsBySignal = new();

        private void Awake()
        {
            RefreshTargets();
        }

        [ContextMenu("맵 연출 대상 다시 찾기")]
        public void RefreshTargets()
        {
            _targetsBySignal.Clear();

            MonoBehaviour[] childBehaviours =
                GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour childBehaviour in childBehaviours)
            {
                if (childBehaviour is not IMapDirectTarget target)
                    continue;

                if (!_targetsBySignal.TryGetValue(
                        target.SignalId,
                        out List<IMapDirectTarget> targets))
                {
                    targets = new List<IMapDirectTarget>();
                    _targetsBySignal.Add(target.SignalId, targets);
                }

                targets.Add(target);
            }
        }

        public void PlayDirect(int signalId)
        {
            TryPlayDirect(signalId);
        }

        public bool TryPlayDirect(int signalId)
        {
            if (!_targetsBySignal.TryGetValue(
                    signalId,
                    out List<IMapDirectTarget> targets))
            {
                return false;
            }

            bool playedAnyTarget = false;
            foreach (IMapDirectTarget target in targets)
            {
                if (target is Object unityObject && unityObject == null)
                    continue;

                target.PlayDirect();
                playedAnyTarget = true;
            }

            return playedAnyTarget;
        }
    }
}
