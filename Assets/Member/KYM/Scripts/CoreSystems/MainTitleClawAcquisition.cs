using System.Collections;
using KimLIb.EventSystem;
using Member.KYM.Scripts.Players;
using Member.KYM.Scripts.Players.RobotArm;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Member.KYM.Scripts.CoreSystems
{
    public sealed class MainTitleClawAcquisition : MonoBehaviour
    {
        [Header("플레이어와 튜토보스의 일반 대화창")]
        [SerializeField] private EventChannelSO dialogChannel;
        [SerializeField] private DialogDataSO dialogData;
        [Header("집게발 획득 후 다시 말을 걸 때의 대화")]
        [SerializeField] private DialogDataSO afterAcquisitionDialogData;

        [Header("대화 카메라와 전체 카메라")]
        [SerializeField] private CinemachineCamera dialogCamera;
        [SerializeField] private CinemachineCamera fullCamera;
        [SerializeField] private int activePriority = 20;
        [SerializeField] private int inactivePriority;

        [Header("집게발 장착과 획득 UI")]
        [SerializeField] private RobotArmEquipmentController equipment;
        [SerializeField] private GameObject acquiredUI;
        [SerializeField, Min(0f)] private float acquiredUIDuration = 2f;

        [Header("두 번째 대화 후 보스 퇴장")]
        [SerializeField] private Npc titleBoss;
        [FormerlySerializedAs("bossToMove")]
        [SerializeField] private Transform bossDestination;
        [SerializeField, Min(0f)] private float moveDuration = 6f;
        [FormerlySerializedAs("onBossMoveFinished")]
        [SerializeField] private UnityEvent onBossMoveStarted;

        private bool _running;
        private bool _acquired;
        private bool _departed;
        private Coroutine _moveRoutine;
        private AgentMover _bossMover;
        private int _previousDialogPriority;
        private int _previousFullPriority;

        private void Awake()
        {
            if (acquiredUI != null) acquiredUI.SetActive(false);
        }

        // PlayerInteractor.OnSuccess 등의 UnityEvent에 연결한다.
        public void Begin()
        {
            if (!isActiveAndEnabled || _running || _departed ||
                DialogManager.Talking || BubbleDialogManager.Talking)
                return;
            var data = _acquired ? afterAcquisitionDialogData : dialogData;
            if (dialogChannel == null || data == null ||
                data.DialogList == null || data.DialogList.Count == 0 ||
                dialogCamera == null || fullCamera == null || dialogCamera == fullCamera ||
                (!_acquired && (equipment == null || acquiredUI == null)) ||
                activePriority <= inactivePriority)
            {
                Debug.LogWarning("집게발 획득 연출의 대화, 카메라, 장착 대상, UI와 우선순위를 확인해주세요.", this);
                return;
            }

            _running = true;
            _previousDialogPriority = dialogCamera.Priority;
            _previousFullPriority = fullCamera.Priority;
            dialogCamera.Priority = activePriority;
            fullCamera.Priority = inactivePriority;
            dialogChannel.AddListener<EndDialogEvent>(OnDialogFinished);
            dialogChannel.RaiseEvent(new StartDialogEvent().InitData(data));
            if (!DialogManager.Talking)
            {
                Debug.LogWarning("대화를 시작하지 못했습니다. DialogManager의 일반 대화 채널을 확인해주세요.", this);
                dialogChannel.RemoveListener<EndDialogEvent>(OnDialogFinished);
                RestoreCameras();
                _running = false;
                return;
            }
        }

        private void OnDialogFinished(EndDialogEvent evt)
        {
            if (!_running || !isActiveAndEnabled) return;
            dialogChannel.RemoveListener<EndDialogEvent>(OnDialogFinished);
            if (_acquired)
            {
                ReturnToFullCamera();
                MoveBossAway();
                return;
            }
            _acquired = true;
            equipment.Equip();
            acquiredUI.SetActive(true);
            StartCoroutine(FinishAcquisition());
        }

        private IEnumerator FinishAcquisition()
        {
            yield return new WaitForSecondsRealtime(acquiredUIDuration);
            acquiredUI.SetActive(false);
            ReturnToFullCamera();
        }

        private void ReturnToFullCamera()
        {
            dialogCamera.Priority = inactivePriority;
            fullCamera.Priority = activePriority;
            _running = false;
        }

        private void MoveBossAway()
        {
            if (titleBoss == null || bossDestination == null)
            {
                Debug.LogWarning("타이틀 보스와 퇴장 목표 Transform을 연결해주세요.", this);
                return;
            }

            _bossMover = titleBoss.GetComponentInChildren<AgentMover>(true);
            if (_bossMover == null || _bossMover.RigidBody == null)
            {
                Debug.LogWarning("타이틀 보스의 AgentMover가 초기화되지 않았습니다.", this);
                return;
            }

            _departed = true;
            float destinationX = bossDestination.position.x;
            float direction = Mathf.Sign(destinationX - _bossMover.RigidBody.position.x);
            var animator = titleBoss.GetComponentInChildren<Animator>(true);
            _bossMover.SetMovementX(direction);
            if (animator != null) animator.Play("MOVE");
            onBossMoveStarted?.Invoke();
            _moveRoutine = StartCoroutine(MoveUntilDestination(destinationX, direction, animator));
        }

        private IEnumerator MoveUntilDestination(float destinationX, float direction, Animator animator)
        {
            float elapsed = 0f;
            while (elapsed < moveDuration &&
                   direction * (destinationX - _bossMover.RigidBody.position.x) > 0.05f)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }

            StopBossMovement(animator);
            _moveRoutine = null;
        }

        private void StopBossMovement(Animator animator)
        {
            if (_bossMover != null)
                _bossMover.StopImmediately(true, false);
            if (animator != null)
                animator.Play("IDLE");
        }

        private void RestoreCameras()
        {
            if (dialogCamera != null) dialogCamera.Priority = _previousDialogPriority;
            if (fullCamera != null) fullCamera.Priority = _previousFullPriority;
        }

        private void OnDisable()
        {
            if (_moveRoutine != null)
            {
                StopCoroutine(_moveRoutine);
                _moveRoutine = null;
                StopBossMovement(titleBoss != null ? titleBoss.GetComponentInChildren<Animator>(true) : null);
            }
            if (dialogChannel != null)
                dialogChannel.RemoveListener<EndDialogEvent>(OnDialogFinished);
            if (!_running) return;
            _running = false;
            StopAllCoroutines();
            if (acquiredUI != null) acquiredUI.SetActive(false);
            RestoreCameras();
        }
    }
}
