using UnityEngine;
using UnityEngine.UI;

namespace Member.KYM.Scripts.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class TutorialSpotlightUI : MonoBehaviour
    {
        [Header("스포트라이트 전용 머티리얼")]
        [SerializeField] private Material spotlightMaterial;
        [Header("강조 대상 (UI 또는 월드 오브젝트)")]
        [SerializeField] private Transform target;
        [SerializeField] private Camera worldCamera;
        [Header("대상이 없을 때 화면 위치 (0~1)")]
        [SerializeField] private Vector2 screenPosition = new(0.5f, 0.5f);
        [Header("원 크기와 오프셋 (화면 픽셀 단위)")]
        [SerializeField, Min(0f)] private float radius = 100f;
        [SerializeField, Min(0f)] private float softness = 15f;
        [SerializeField] private Vector2 screenOffset;
        [Header("배경 및 표시")]
        [SerializeField] private Color backgroundColor = new(0f, 0f, 0f, 0.7f);
        [SerializeField] private bool showOnStart;
        [SerializeField] private bool blockInput;

        private static readonly int CenterId = Shader.PropertyToID("_SpotlightCenter");
        private static readonly int ScreenSizeId = Shader.PropertyToID("_SpotlightScreenSize");
        private static readonly int RadiusId = Shader.PropertyToID("_SpotlightRadius");
        private static readonly int SoftnessId = Shader.PropertyToID("_SpotlightSoftness");
        private Image _image;
        private Material _material;
        private Material _originalMaterial;
        private Color _originalColor;
        private bool _originalRaycastTarget;
        private bool _visible;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _originalMaterial = _image.material;
            _originalColor = _image.color;
            _originalRaycastTarget = _image.raycastTarget;
            var source = spotlightMaterial != null ? spotlightMaterial : _originalMaterial;
            if (source == null || !source.HasProperty(CenterId))
            {
                Debug.LogError("TutorialSpotlight 셰이더를 사용하는 머티리얼을 연결해주세요.", this);
                _image.enabled = false;
                enabled = false;
                return;
            }
            _material = new Material(source);
            _image.material = _material;
            _visible = showOnStart;
            Refresh();
        }

        private void OnEnable()
        {
            if (_material != null) Refresh();
        }

        private void LateUpdate()
        {
            if (_visible) Refresh();
        }

        public void Show()
        {
            if (_material == null) return;
            _visible = true;
            Refresh();
        }

        public void Hide()
        {
            _visible = false;
            if (_image == null) return;
            _image.enabled = false;
            _image.raycastTarget = false;
        }

        public void ShowTarget(Transform value)
        {
            target = value;
            Show();
        }

        public void ShowUI(RectTransform value) => ShowTarget(value);

        // 대상 추적을 해제하고 화면의 정규화 좌표를 강조한다.
        public void ShowAt(Vector2 normalizedPosition)
        {
            target = null;
            screenPosition = normalizedPosition;
            Show();
        }

        public void SetRadius(float pixels)
        {
            radius = Mathf.Max(0f, pixels);
            if (_material != null) Refresh();
        }

        private void Refresh()
        {
            _image.enabled = _visible && isActiveAndEnabled;
            _image.raycastTarget = _image.enabled && blockInput;
            _image.color = backgroundColor;
            float width = Mathf.Max(1, Screen.width);
            float height = Mathf.Max(1, Screen.height);
            Vector2 center = new(screenPosition.x * width, screenPosition.y * height);
            if (target is RectTransform rect)
            {
                var canvas = rect.GetComponentInParent<Canvas>();
                var root = canvas != null ? canvas.rootCanvas : null;
                var camera = root != null && root.renderMode != RenderMode.ScreenSpaceOverlay
                    ? root.worldCamera : null;
                center = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            }
            else if (target != null)
            {
                var camera = worldCamera != null ? worldCamera : Camera.main;
                if (camera == null)
                {
                    _image.enabled = false;
                    _image.raycastTarget = false;
                    return;
                }
                Vector3 point = camera.WorldToScreenPoint(target.position);
                center = point.z > 0f ? (Vector2)point : new Vector2(-width * 10f, -height * 10f);
            }
            center += screenOffset;
            _material.SetVector(CenterId, new Vector4(center.x / width, center.y / height, 0f, 0f));
            _material.SetVector(ScreenSizeId, new Vector4(width, height, 0f, 0f));
            _material.SetFloat(RadiusId, Mathf.Max(0f, radius));
            _material.SetFloat(SoftnessId, Mathf.Max(0f, softness));
        }

        private void OnDisable()
        {
            if (_image == null) return;
            _image.enabled = false;
            _image.raycastTarget = false;
        }

        private void OnDestroy()
        {
            if (_image != null)
            {
                _image.material = _originalMaterial;
                _image.color = _originalColor;
                _image.raycastTarget = _originalRaycastTarget;
            }
            if (_material != null) Destroy(_material);
        }
    }
}
