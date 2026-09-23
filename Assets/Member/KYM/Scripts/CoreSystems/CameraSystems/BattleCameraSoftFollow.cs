using Unity.Cinemachine;
using UnityEngine;

namespace Member.KYM.Scripts.CoreSystems.CameraSystems
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineCamera))]
    public class BattleCameraSoftFollow : MonoBehaviour
    {
        [Header("카메라 이동 범위")]
        [SerializeField] private Vector2 maxOffset = new(0.5f, 0.3f);
        [SerializeField, Min(0f)] private float smoothTime = 0.55f;

        [Header("경계가 없을 때 사용할 전투 영역")]
        [SerializeField] private Vector2 fallbackArenaHalfSize = new(12f, 6.75f);

        private CinemachineCamera _camera;
        private CinemachineConfiner2D _confiner;
        private Transform _player;
        private Transform _focusTarget;
        private Vector2 _battleCenter;
        private Vector2 _arenaHalfSize;
        private Vector2 _smoothVelocity;

        private void Awake()
        {
            _camera = GetComponent<CinemachineCamera>();
            _confiner = GetComponent<CinemachineConfiner2D>();
            _player = _camera.Target.TrackingTarget;

            ResolveBattleArea();
            CreateFocusTarget();

            if (_player == null)
            {
                Debug.LogWarning(
                    "BattleCam에 플레이어 Tracking Target이 할당되지 않았습니다.",
                    this);
            }
        }

        private void OnEnable()
        {
            if (_camera != null && _focusTarget != null)
                _camera.Target.TrackingTarget = _focusTarget;
        }

        private void Update()
        {
            if (_player == null || _focusTarget == null)
                return;

            Vector2 playerOffset = (Vector2)_player.position - _battleCenter;
            Vector2 normalizedOffset = new(
                Mathf.Clamp(playerOffset.x / _arenaHalfSize.x, -1f, 1f),
                Mathf.Clamp(playerOffset.y / _arenaHalfSize.y, -1f, 1f));

            Vector2 targetPosition = _battleCenter + new Vector2(
                normalizedOffset.x * maxOffset.x,
                normalizedOffset.y * maxOffset.y);

            Vector2 currentPosition = _focusTarget.position;
            Vector2 smoothedPosition = smoothTime <= 0f
                ? targetPosition
                : Vector2.SmoothDamp(
                    currentPosition,
                    targetPosition,
                    ref _smoothVelocity,
                    smoothTime);

            _focusTarget.position = new Vector3(
                smoothedPosition.x,
                smoothedPosition.y,
                _player.position.z);
        }

        private void OnDisable()
        {
            RestorePlayerTarget();
        }

        private void OnDestroy()
        {
            RestorePlayerTarget();

            if (_focusTarget != null)
                Destroy(_focusTarget.gameObject);
        }

        private void ResolveBattleArea()
        {
            Collider2D boundary = _confiner != null
                ? _confiner.BoundingShape2D
                : null;

            if (boundary != null)
            {
                Bounds bounds = boundary.bounds;
                _battleCenter = bounds.center;
                _arenaHalfSize = new Vector2(
                    Mathf.Max(0.01f, bounds.extents.x),
                    Mathf.Max(0.01f, bounds.extents.y));
                return;
            }

            _battleCenter = transform.position;
            _arenaHalfSize = new Vector2(
                Mathf.Max(0.01f, fallbackArenaHalfSize.x),
                Mathf.Max(0.01f, fallbackArenaHalfSize.y));
        }

        private void CreateFocusTarget()
        {
            GameObject focusObject = new("Battle Camera Focus Target")
            {
                hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave
            };

            _focusTarget = focusObject.transform;
            float targetZ = _player != null ? _player.position.z : 0f;
            _focusTarget.position = new Vector3(
                _battleCenter.x,
                _battleCenter.y,
                targetZ);
        }

        private void RestorePlayerTarget()
        {
            if (_camera != null &&
                _focusTarget != null &&
                _camera.Target.TrackingTarget == _focusTarget)
            {
                _camera.Target.TrackingTarget = _player;
            }
        }

        private void OnValidate()
        {
            maxOffset = new Vector2(
                Mathf.Max(0f, maxOffset.x),
                Mathf.Max(0f, maxOffset.y));
            smoothTime = Mathf.Max(0f, smoothTime);
            fallbackArenaHalfSize = new Vector2(
                Mathf.Max(0.01f, fallbackArenaHalfSize.x),
                Mathf.Max(0.01f, fallbackArenaHalfSize.y));
        }
    }
}
