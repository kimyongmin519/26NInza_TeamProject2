using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.Players;
using Member.KYM.Scripts.Players.RobotArm;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Member.KYM.Scripts.CoreSystems
{
    public sealed class PlayerTransitionTeleporter : MonoBehaviour
    {
        [Header("플레이어와 같은 씬의 도착 위치")]
        [SerializeField] private PlayerController player;
        [SerializeField] private Transform destination;

        [Header("직접 연결할 공용 트랜지션 UI")]
        [SerializeField] private GameObject transitionCanvas;
        [SerializeField] private Image fadeImage;
        [SerializeField] private float openCircleSize = 2.5f;
        [SerializeField] private float closedCircleSize;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.75f;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.75f;

        private static readonly int CircleSizeId = Shader.PropertyToID("_CircleSize");
        private Sequence _sequence;
        private Material _originalMaterial;
        private Material _transitionMaterial;
        private bool _originalRaycastTarget;
        private bool _originalCanvasActive;

        private PlayerInputSO _lockedInput;
        private readonly bool[] _previousLocks = new bool[(int)global::LockKey.END];
        private bool _isTeleporting;

        // 버튼, 상호작용 등의 UnityEvent에 연결한다.
        public void Teleport()
        {
            if (!isActiveAndEnabled || _isTeleporting || destination == null)
                return;

            if (player == null)
                player = FindFirstObjectByType<PlayerController>();

            if (player == null)
                return;

            if (transitionCanvas == null || fadeImage == null ||
                fadeImage.material == null || !fadeImage.material.HasProperty(CircleSizeId))
            {
                Debug.LogError("트랜지션 Canvas와 _CircleSize 셰이더를 사용하는 Image를 연결해주세요.", this);
                return;
            }

            _isTeleporting = true;
            _originalCanvasActive = transitionCanvas.activeSelf;
            transitionCanvas.SetActive(true);
            // 공용 UI의 기존 머티리얼은 수정하지 않고 전환 동안만 복제본을 사용한다.
            _originalMaterial = fadeImage.material;
            _originalRaycastTarget = fadeImage.raycastTarget;
            _transitionMaterial = new Material(_originalMaterial);
            fadeImage.material = _transitionMaterial;
            fadeImage.raycastTarget = true;
            _transitionMaterial.SetFloat(CircleSizeId, openCircleSize);
            _lockedInput = player.PlayerInput;
            if (_lockedInput != null)
            {
                for (int i = 0; i < _previousLocks.Length; i++)
                    _previousLocks[i] = _lockedInput.IsInputLocked((global::LockKey)i);
                _lockedInput.AllInputLock(true);
            }

            _sequence = DOTween.Sequence().SetUpdate(true);
            _sequence.Append(_transitionMaterial.DOFloat(closedCircleSize, CircleSizeId, fadeOutDuration)
                .SetEase(Ease.Linear));
            _sequence.AppendCallback(MovePlayer);
            _sequence.Append(_transitionMaterial.DOFloat(openCircleSize, CircleSizeId, fadeInDuration)
                .SetEase(Ease.Linear));
            _sequence.OnComplete(FinishTeleport);
        }

        public void TeleportTo(Transform target)
        {
            if (_isTeleporting) return;
            destination = target;
            Teleport();
        }

        private void MovePlayer()
        {
            if (!_isTeleporting || !isActiveAndEnabled || player == null || destination == null)
                return;

            var skill = player.SkillModule?.GetCurrentSkill();
            if (skill != null && skill.IsUsing)
                skill.StopSkill();

            player.GetComponentInChildren<RobotArmGrappler>(true)?.StopGrapple();
            IMover mover = player.GetModule<IMover>();
            mover?.StopImmediately(true, true);
            Vector3 positionDelta = destination.position - player.transform.position;
            player.transform.position = destination.position;
            if (mover != null && mover.RigidBody != null)
                mover.RigidBody.position = destination.position;
            player.ResetJumpCount();
            Physics2D.SyncTransforms();
            Unity.Cinemachine.CinemachineCore.OnTargetObjectWarped(player.transform, positionDelta);
        }

        private void FinishTeleport()
        {
            if (!_isTeleporting) return;
            if (fadeImage != null && fadeImage.material == _transitionMaterial)
            {
                fadeImage.material = _originalMaterial;
                fadeImage.raycastTarget = _originalRaycastTarget;
            }
            if (_transitionMaterial != null)
                Destroy(_transitionMaterial);
            _transitionMaterial = null;
            if (_lockedInput != null)
            {
                for (int i = 0; i < _previousLocks.Length; i++)
                    _lockedInput.LockInput((global::LockKey)i, _previousLocks[i]);
            }
            _lockedInput = null;
            _isTeleporting = false;
            if (transitionCanvas != null)
                transitionCanvas.SetActive(_originalCanvasActive);
        }

        private void OnDisable()
        {
            _sequence?.Kill();
            FinishTeleport();
        }
    }
}
