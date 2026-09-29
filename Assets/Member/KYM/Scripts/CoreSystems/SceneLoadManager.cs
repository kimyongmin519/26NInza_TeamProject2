using System.Collections;
using KimLIb.EventSystem;
using Member.KYM.Scripts.CoreSystems.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Member.KYM.Scripts.CoreSystems
{
    public sealed class SceneLoadManager : KimLIb.MonoSingleton<SceneLoadManager>
    {
        [Header("공용 화면 전환 채널")]
        [SerializeField] private EventChannelSO transitionChannel;
        public bool IsTransitioning { get; private set; }

        public static bool TryLoadScene(string sceneName)
        {
            var manager = Instance;
            if (manager == null || !manager.isActiveAndEnabled || manager.IsTransitioning) return false;
            if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"씬 '{sceneName}'을 빌드 씬 목록에 등록해주세요.");
                return false;
            }
            manager.IsTransitioning = true;
            bool accepted = TransitionRequest.TryRaise(manager.transitionChannel,
                () => LoadAsync(sceneName), () => manager.IsTransitioning = false);
            if (!accepted) manager.IsTransitioning = false;
            return accepted;
        }

        private static IEnumerator LoadAsync(string sceneName)
        {
            yield return SceneManager.LoadSceneAsync(sceneName);
        }

        public static bool TryReloadCurrentScene() => TryLoadScene(SceneManager.GetActiveScene().path);
        public void LoadScene(string sceneName) => TryLoadScene(sceneName);
        public void ReloadCurrentScene() => TryReloadCurrentScene();
    }
}
