using Member.KYM.Scripts.Players;
using Member.KYM.Scripts.Players.RobotArm;
using UnityEngine;

namespace Member.KYM.Scripts.CombatSystems.Projectiles
{
    [DisallowMultipleComponent]
    public class ProjectileGrabCue : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sourceRenderer;
        [SerializeField] private SpriteRenderer cueRenderer;

        [Header("잡기 표시")]
        [SerializeField, Range(0f, 1f)] private float maxOpacity = 0.5f;

        private IGrabbable _grabbable;
        private PlayerController _player;
        private static Material _sharedCueMaterial;

        private void Awake()
        {
            _grabbable = GetComponent<IGrabbable>();
            if (sourceRenderer == null)
                sourceRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (cueRenderer == null && sourceRenderer != null)
                CreateCueRenderer();
            _player = FindFirstObjectByType<PlayerController>();
            HideCue();
        }

        private void CreateCueRenderer()
        {
            Shader shader = Shader.Find("KYM/2D/Projectile Grab Cue");
            if (shader == null)
                return;

            if (_sharedCueMaterial == null)
                _sharedCueMaterial = new Material(shader);

            GameObject cueObject = new GameObject("GrabCue");
            cueObject.transform.SetParent(sourceRenderer.transform, false);
            cueRenderer = cueObject.AddComponent<SpriteRenderer>();
            cueRenderer.sharedMaterial = _sharedCueMaterial;
        }

        private void LateUpdate()
        {
            if (_grabbable == null || !_grabbable.CanBeGrabbed ||
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
