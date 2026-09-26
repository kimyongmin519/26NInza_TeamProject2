using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Member.KYM.Scripts.CoreSystems
{
    public sealed class SceneLoadManager : KimLIb.MonoSingleton<SceneLoadManager>
    {
        [Header("씬에 배치된 전환 UI")]
        [SerializeField] private GameObject transitionCanvas;
        [SerializeField] private Image fadeImage;
        [SerializeField] private float openCircleSize = 2.5f;
        [SerializeField] private float closedCircleSize;

        [Header("전환 시간")]
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.75f;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.75f;

        public bool IsTransitioning { get; private set; }

        private static readonly int CircleSizeId = Shader.PropertyToID("_CircleSize");
        private Material _fadeMaterial;

        protected override void Awake()
        {
            base.Awake();
            if (!IsSingletonInstance)
                return;

            if (transitionCanvas == null || fadeImage == null ||
                !fadeImage.material.HasProperty(CircleSizeId))
            {
                Debug.LogError("TransitionCanvas와 전환 Image의 원형 페이드 머티리얼을 연결해주세요.", this);
                enabled = false;
                return;
            }

            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            transitionCanvas.transform.SetParent(null);
            DontDestroyOnLoad(transitionCanvas);
        }

        private void Start()
        {
            if (!IsSingletonInstance)
                return;

            // FadeScreenManager의 Awake가 이미지 머티리얼을 교체한 뒤 초기화한다.
            _fadeMaterial = new Material(fadeImage.material);
            fadeImage.material = _fadeMaterial;
            _fadeMaterial.SetFloat(CircleSizeId, openCircleSize);
            fadeImage.raycastTarget = false;
        }

        public static bool TryLoadScene(string sceneName)
        {
            if (Instance == null || !Instance.isActiveAndEnabled ||
                Instance._fadeMaterial == null || Instance.IsTransitioning)
                return false;

            if (string.IsNullOrWhiteSpace(sceneName) ||
                !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"씬 '{sceneName}'을 빌드 씬 목록에 등록해주세요.");
                return false;
            }

            Instance.StartCoroutine(Instance.Transition(sceneName));
            return true;
        }

        public static bool TryReloadCurrentScene()
        {
            return TryLoadScene(SceneManager.GetActiveScene().path);
        }

        // UnityEvent에서 씬 이름을 입력해 연결한다.
        public void LoadScene(string sceneName)
        {
            TryLoadScene(sceneName);
        }

        public void ReloadCurrentScene()
        {
            TryReloadCurrentScene();
        }

        private IEnumerator Transition(string sceneName)
        {
            IsTransitioning = true;
            fadeImage.raycastTarget = true;

            yield return _fadeMaterial.DOFloat(closedCircleSize, CircleSizeId, fadeOutDuration)
                .SetEase(Ease.Linear).SetUpdate(true).WaitForCompletion();

            yield return SceneManager.LoadSceneAsync(sceneName);

            yield return _fadeMaterial.DOFloat(openCircleSize, CircleSizeId, fadeInDuration)
                .SetEase(Ease.Linear).SetUpdate(true).WaitForCompletion();

            fadeImage.raycastTarget = false;
            IsTransitioning = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_fadeMaterial != null)
            {
                _fadeMaterial.DOKill();
                Destroy(_fadeMaterial);
            }
        }
    }
}
