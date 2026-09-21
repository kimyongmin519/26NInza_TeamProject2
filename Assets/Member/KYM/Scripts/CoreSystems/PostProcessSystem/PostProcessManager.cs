using System.Collections.Generic;
using KimLIb.EventSystem;
using Member.KYM.Scripts.CoreSystems.Events;
using UnityEngine;

namespace Member.KYM.Scripts.CoreSystems.PostProcessSystem
{
    public class PostProcessManager : MonoBehaviour
    {
        [Header("이벤트")]
        [SerializeField] private EventChannelSO postProcessChannel;

        private readonly Dictionary<PostProcessType, IPostProcessEffect> _effects = new();
        private bool _isSubscribed;

        private void Awake()
        {
            RefreshEffects();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        [ContextMenu("자식 PP 다시 찾기")]
        public void RefreshEffects()
        {
            bool shouldResubscribe = isActiveAndEnabled;
            Unsubscribe();
            _effects.Clear();

            MonoBehaviour[] childBehaviours =
                GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour childBehaviour in childBehaviours)
            {
                if (childBehaviour is not IPostProcessEffect effect)
                    continue;

                if (_effects.TryAdd(effect.Type, effect))
                    continue;

                Debug.LogWarning(
                    $"{effect.Type} PP가 중복되어 첫 번째 컴포넌트만 사용합니다.",
                    childBehaviour);
            }

            if (shouldResubscribe)
                Subscribe();
        }

        private void Subscribe()
        {
            if (_isSubscribed || postProcessChannel == null || _effects.Count == 0)
                return;

            postProcessChannel.AddListener<PostProcessRequestEvent>(
                HandlePostProcessRequest);
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || postProcessChannel == null)
                return;

            postProcessChannel.RemoveListener<PostProcessRequestEvent>(
                HandlePostProcessRequest);
            _isSubscribed = false;
        }

        private void HandlePostProcessRequest(PostProcessRequestEvent evt)
        {
            if (evt == null || !_effects.TryGetValue(evt.Type, out IPostProcessEffect effect))
                return;

            effect.Handle(evt.Request);
        }
    }
}
