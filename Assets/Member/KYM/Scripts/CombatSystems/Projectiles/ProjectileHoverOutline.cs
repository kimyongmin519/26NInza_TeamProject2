using Member.KYM.Scripts.CoreSystems;
using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.Projectiles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GrabbableProjectile))]
    public class ProjectileHoverOutline : MonoBehaviour
    {
        [SerializeField] private PlayerInputSO playerInput;
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Material grabbableOutline;
        [SerializeField] private Material ungrabbableOutline;

        private GrabbableProjectile _projectile;
        private Collider2D _collider;
        private Camera _mainCamera;
        private Material _originalMaterial;
        private Material _activeOutline;

        private void Awake()
        {
            _projectile = GetComponent<GrabbableProjectile>();
            _collider = GetComponent<Collider2D>();
            _mainCamera = Camera.main;

            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<SpriteRenderer>(true);

            if (targetRenderer != null)
                _originalMaterial = targetRenderer.sharedMaterial;
        }

        private void Update()
        {
            if (playerInput == null || _projectile == null ||
                _collider == null || targetRenderer == null)
                return;

            if (_mainCamera == null)
                _mainCamera = Camera.main;

            if (_mainCamera == null || !_collider.enabled)
            {
                SetOutline(null);
                return;
            }

            Vector3 mousePosition = playerInput.MousePos;
            mousePosition.z = Mathf.Abs(
                _mainCamera.transform.position.z - transform.position.z);
            Vector2 mouseWorldPosition = _mainCamera.ScreenToWorldPoint(mousePosition);

            Material outline = null;
            if (_collider.OverlapPoint(mouseWorldPosition))
            {
                outline = _projectile.CanBeGrabbed
                    ? grabbableOutline
                    : ungrabbableOutline;
            }

            SetOutline(outline);
        }

        private void OnDisable()
        {
            SetOutline(null);
        }

        private void SetOutline(Material outline)
        {
            if (targetRenderer == null || _activeOutline == outline)
                return;

            targetRenderer.sharedMaterial = outline != null
                ? outline
                : _originalMaterial;
            _activeOutline = outline;
        }
    }
}
