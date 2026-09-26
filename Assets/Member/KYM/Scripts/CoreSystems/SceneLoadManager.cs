using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Member.KYM.Scripts.CoreSystems
{
    public sealed class SceneLoadManager : MonoBehaviour
    {
        private const string PrefabPath = "SceneTransitionCanvas";
        private static readonly int CircleSizeId = Shader.PropertyToID("_CircleSize");

        [Header("팀원 전환 UI")]
        [SerializeField] private Image fadeImage;
        [SerializeField] private float openCircleSize = 2.5f;
        [SerializeField] private float closedCircleSize;

        [Header("전환 시간")]
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.75f;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.75f;

        public static SceneLoadManager Instance { get; private set; }
        public bool IsTransitioning { get; private set; }

        private Material _fadeMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance() => Instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateInstance()
        {
            GameObject prefab = Resources.Load<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Resources/{PrefabPath} 전환 UI 프리팹을 찾지 못했습니다.");
                return;
            }

            Instantiate(prefab);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (fadeImage == null || fadeImage.material == null ||
                !fadeImage.material.HasProperty(CircleSizeId))
            {
                Debug.LogError("씬 전환 Image에 팀원의 원형 페이드 머티리얼을 연결해야 합니다.", this);
                enabled = false;
                return;
            }

            _fadeMaterial = new Material(fadeImage.material);
            fadeImage.material = _fadeMaterial;
            SetCircleSize(openCircleSize);
            fadeImage.raycastTarget = false;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (_fadeMaterial != null)
                Destroy(_fadeMaterial);
        }

        public static bool TryLoadScene(string sceneName)
        {
            if (Instance == null || !Instance.isActiveAndEnabled)
            {
                Debug.LogError("씬 전환 매니저가 준비되지 않았습니다.");
                return false;
            }

            return Instance.BeginTransition(sceneName);
        }

        public static bool TryReloadCurrentScene()
        {
            return TryLoadScene(SceneManager.GetActiveScene().path);
        }

        private bool BeginTransition(string sceneName)
        {
            if (IsTransitioning)
                return false;

            if (string.IsNullOrWhiteSpace(sceneName) ||
                !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"전환할 씬 '{sceneName}'이 빌드 씬 목록에 없습니다.", this);
                return false;
            }

            StartCoroutine(Transition(sceneName));
            return true;
        }

        private IEnumerator Transition(string sceneName)
        {
            IsTransitioning = true;
            fadeImage.raycastTarget = true;

            yield return Fade(openCircleSize, closedCircleSize, fadeOutDuration);

            AsyncOperation loadOperation = null;
            try
            {
                loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }

            if (loadOperation == null)
            {
                yield return Fade(closedCircleSize, openCircleSize, fadeInDuration);
                FinishTransition();
                yield break;
            }

            while (!loadOperation.isDone)
                yield return null;

            yield return Fade(closedCircleSize, openCircleSize, fadeInDuration);
            FinishTransition();
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            SetCircleSize(from);
            if (duration <= 0f)
            {
                SetCircleSize(to);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetCircleSize(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            SetCircleSize(to);
        }

        private void SetCircleSize(float value)
        {
            if (_fadeMaterial != null)
                _fadeMaterial.SetFloat(CircleSizeId, value);
        }

        private void FinishTransition()
        {
            fadeImage.raycastTarget = false;
            IsTransitioning = false;
        }
    }
}
