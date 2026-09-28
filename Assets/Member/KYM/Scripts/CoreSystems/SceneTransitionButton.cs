using UnityEngine;

namespace Member.KYM.Scripts.CoreSystems
{
    public sealed class SceneTransitionButton : MonoBehaviour
    {
        [SerializeField] private string destinationScene;

        public void LoadDestination()
        {
            SceneLoadManager.TryLoadScene(destinationScene);
        }

        public void ReloadCurrentScene()
        {
            SceneLoadManager.TryReloadCurrentScene();
        }
    }
}
