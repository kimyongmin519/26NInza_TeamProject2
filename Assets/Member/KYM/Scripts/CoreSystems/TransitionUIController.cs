using System.Collections;
using DG.Tweening;
using KimLIb.EventSystem;
using Member.KYM.Scripts.CoreSystems.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Member.KYM.Scripts.CoreSystems
{
    // 활성화된 TransitionCanvas 루트에 배치한다.
    public sealed class TransitionUIController : MonoBehaviour
    {
        [Header("공용 전환 채널과 이미지")]
        [SerializeField] private EventChannelSO transitionChannel;
        [SerializeField] private Image fadeImage;
        [Header("다른 UI보다 위에 표시할 순서")]
        [SerializeField] private int sortingOrder = 1000;
        [Header("원형 전환 크기와 시간")]
        [SerializeField] private float openCircleSize = 2.5f;
        [SerializeField] private float closedCircleSize;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.75f;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.75f;

        private static TransitionUIController _instance;
        private static readonly int CircleSizeId = Shader.PropertyToID("_CircleSize");
        private Material _material;
        private Material _originalMaterial;
        private TransitionRequest _request;

        private void Awake()
        {
            // 기존 시스템 프리팹의 페이드가 같은 이미지를 동시에 열지 않도록 한다.
            // 런타임 컴포넌트만 제거하며 팀원 스크립트/프리팹은 수정하지 않는다.
            foreach (var legacyFade in GetComponents<global::FadeScreenManager>())
            {
                legacyFade.StopAllCoroutines();
                legacyFade.enabled = false;
                Destroy(legacyFade);
            }

            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (_instance != this) return;
            if (transitionChannel == null || fadeImage == null || fadeImage.material == null ||
                !fadeImage.material.HasProperty(CircleSizeId))
            {
                Debug.LogError("전환 채널과 _CircleSize 머티리얼을 사용하는 Image를 연결해주세요.", this);
                return;
            }
            _originalMaterial = fadeImage.material;
            _material = new Material(_originalMaterial);
            fadeImage.material = _material;
            var canvas = fadeImage.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = sortingOrder;
            }
            ResetScreen();
            transitionChannel.AddListener<TransitionRequest>(Handle);
        }

        private void Handle(TransitionRequest request)
        {
            if (!isActiveAndEnabled || _material == null || _request != null || request.Accepted) return;
            request.Accepted = true;
            _request = request;
            StartCoroutine(Play(request));
        }

        private IEnumerator Play(TransitionRequest request)
        {
            try
            {
                fadeImage.gameObject.SetActive(true);
                fadeImage.raycastTarget = true;
                yield return _material.DOFloat(closedCircleSize, CircleSizeId, fadeOutDuration)
                    .SetEase(Ease.Linear).SetUpdate(true).WaitForCompletion();
                if (request.Operation != null) yield return request.Operation();

                // 로딩으로 길어진 프레임을 열기 트윈의 첫 deltaTime으로 사용하지 않는다.
                // 새 씬의 Start가 끝난 뒤에도 완전히 가린 화면을 한 프레임 표시한다.
                _material.SetFloat(CircleSizeId, closedCircleSize);
                yield return null;
                _material.SetFloat(CircleSizeId, closedCircleSize);
                yield return null;

                yield return _material.DOFloat(openCircleSize, CircleSizeId, fadeInDuration)
                    .SetEase(Ease.Linear).SetUpdate(true).WaitForCompletion();
            }
            finally { Finish(); }
        }

        private void ResetScreen()
        {
            if (_material != null) _material.SetFloat(CircleSizeId, openCircleSize);
            if (fadeImage != null) fadeImage.raycastTarget = false;
        }

        private void Finish()
        {
            var request = _request;
            _request = null;
            ResetScreen();
            request?.Finished?.Invoke();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (_material != null) _material.DOKill();
            Finish();
        }

        private void OnDestroy()
        {
            if (_instance != this) return;
            _instance = null;
            if (transitionChannel != null) transitionChannel.RemoveListener<TransitionRequest>(Handle);
            if (fadeImage != null && fadeImage.material == _material) fadeImage.material = _originalMaterial;
            if (_material != null) Destroy(_material);
        }
    }
}
