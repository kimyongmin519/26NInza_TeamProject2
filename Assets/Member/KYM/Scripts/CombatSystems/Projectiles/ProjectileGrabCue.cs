using Member.KYM.Scripts.Players;
using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.Projectiles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GrabbableProjectile))]
    public class ProjectileGrabCue : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sourceRenderer;
        [SerializeField] private SpriteRenderer cueRenderer;

        [Header("잡기 표시")]
        [SerializeField, Range(0f, 1f)] private float maxOpacity = 0.5f;

        private GrabbableProjectile _projectile;
        private PlayerController _player;

        private void Awake()
        {
            _projectile = GetComponent<GrabbableProjectile>();
            _player = FindFirstObjectByType<PlayerController>();
            HideCue();
        }

        private void LateUpdate()
        {
            if (_projectile == null || !_projectile.CanBeGrabbed ||
                sourceRenderer == null || !sourceRenderer.enabled ||
                sourceRenderer.sprite == null || cueRenderer == null)
            {
                HideCue();
                return;
            }

            if (_player == null)
                _player = FindFirstObjectByType<PlayerController>();

            if (_player == null)
            {
                HideCue();
                return;
            }

            float distance = Vector2.Distance(transform.position, _player.transform.position);
            float revealDistance = _player.ProjectileCueRevealDistance;
            float fullRevealDistance = _player.ProjectileCueFullRevealDistance;
            if (distance >= revealDistance)
            {
                HideCue();
                return;
            }

            float proximity = revealDistance > fullRevealDistance
                ? Mathf.Clamp01((revealDistance - distance) /
                                (revealDistance - fullRevealDistance))
                : 1f;

            cueRenderer.sprite = sourceRenderer.sprite;
            cueRenderer.flipX = sourceRenderer.flipX;
            cueRenderer.flipY = sourceRenderer.flipY;
            cueRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
            cueRenderer.sortingOrder = sourceRenderer.sortingOrder + 1;
            cueRenderer.color = new Color(1f, 1f, 1f,
                Mathf.SmoothStep(0f, maxOpacity, proximity));
            cueRenderer.enabled = true;
        }

        private void OnDisable()
        {
            HideCue();
        }

        private void HideCue()
        {
            if (cueRenderer != null)
                cueRenderer.enabled = false;
        }

    }
}
