using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Member.KYM.Scripts.CoreSystems.CameraSystems
{
    [DisallowMultipleComponent]
    public sealed class PixelPerfectBlendController : MonoBehaviour
    {
        [Header("메인 카메라 컴포넌트")]
        [SerializeField] private CinemachineBrain brain;
        [SerializeField] private PixelPerfectCamera pixelPerfectCamera;

        private bool _initialEnabled;
        private bool _subscribed;

        private void OnEnable()
        {
            if (brain == null)
                brain = GetComponent<CinemachineBrain>();
            if (pixelPerfectCamera == null)
                pixelPerfectCamera = GetComponent<PixelPerfectCamera>();

            if (brain == null || pixelPerfectCamera == null)
            {
                Debug.LogError("CinemachineBrain과 Pixel Perfect Camera를 연결해주세요.", this);
                return;
            }

            _initialEnabled = pixelPerfectCamera.enabled;
            CinemachineCore.CameraUpdatedEvent.AddListener(HandleCameraUpdated);
            _subscribed = true;
            ApplyPixelPerfect();
        }

        private void HandleCameraUpdated(CinemachineBrain updatedBrain)
        {
            if (updatedBrain == brain)
                ApplyPixelPerfect();
        }

        private void ApplyPixelPerfect()
        {
            // 실제 카메라가 갱신된 뒤, 렌더링 전에 블렌딩 여부를 적용한다.
            pixelPerfectCamera.enabled = _initialEnabled && !brain.IsBlending;
        }

        private void OnDisable()
        {
            if (!_subscribed)
                return;

            CinemachineCore.CameraUpdatedEvent.RemoveListener(HandleCameraUpdated);
            _subscribed = false;
            if (pixelPerfectCamera != null)
                pixelPerfectCamera.enabled = _initialEnabled;
        }
    }
}
