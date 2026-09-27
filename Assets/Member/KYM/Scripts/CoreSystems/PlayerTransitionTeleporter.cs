using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.Players;
using Member.KYM.Scripts.Players.RobotArm;
using System.Collections;
using KimLIb.EventSystem;
using Member.KYM.Scripts.CoreSystems.Events;
using UnityEngine;

namespace Member.KYM.Scripts.CoreSystems
{
    public sealed class PlayerTransitionTeleporter : MonoBehaviour
    {
        [Header("플레이어와 같은 씬의 도착 위치")]
        [SerializeField] private PlayerController player;
        [SerializeField] private Transform destination;

        [Header("공용 화면 전환 채널")]
        [SerializeField] private EventChannelSO transitionChannel;

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

            _isTeleporting = true;
            _lockedInput = player.PlayerInput;
            if (_lockedInput != null)
            {
                for (int i = 0; i < _previousLocks.Length; i++)
                    _previousLocks[i] = _lockedInput.IsInputLocked((global::LockKey)i);
                _lockedInput.AllInputLock(true);
            }

            if (!TransitionRequest.TryRaise(transitionChannel, MoveWhileCovered, FinishTeleport))
                FinishTeleport();
        }

        private IEnumerator MoveWhileCovered()
        {
            MovePlayer();
            yield break;
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
            if (_lockedInput != null)
            {
                for (int i = 0; i < _previousLocks.Length; i++)
                    _lockedInput.LockInput((global::LockKey)i, _previousLocks[i]);
            }
            _lockedInput = null;
            _isTeleporting = false;
        }

        private void OnDisable()
        {
            FinishTeleport();
        }
    }
}
