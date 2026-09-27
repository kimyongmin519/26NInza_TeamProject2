using System.Collections;
using KimLIb.EventSystem;
using Member.KYM.Scripts.Players.RobotArm;
using Unity.Cinemachine;
using UnityEngine;

namespace Member.KYM.Scripts.CoreSystems
{
    public sealed class MainTitleClawAcquisition : MonoBehaviour
    {
        [Header("플레이어와 튜토보스의 일반 대화창")]
        [SerializeField] private EventChannelSO dialogChannel;
        [SerializeField] private DialogDataSO dialogData;

        [Header("대화 카메라와 전체 카메라")]
        [SerializeField] private CinemachineCamera dialogCamera;
        [SerializeField] private CinemachineCamera fullCamera;
        [SerializeField] private int activePriority = 20;
        [SerializeField] private int inactivePriority;

        [Header("집게발 장착과 획득 UI")]
        [SerializeField] private RobotArmEquipmentController equipment;
        [SerializeField] private GameObject acquiredUI;
        [SerializeField, Min(0f)] private float acquiredUIDuration = 2f;

        private bool _running;
        private bool _acquired;
        private int _previousDialogPriority;
        private int _previousFullPriority;

        private void Awake()
        {
            if (acquiredUI != null) acquiredUI.SetActive(false);
        }

        // PlayerInteractor.OnSuccess 등의 UnityEvent에 연결한다.
        public void Begin()
        {
            if (!isActiveAndEnabled || _running || _acquired ||
                DialogManager.Talking || BubbleDialogManager.Talking)
                return;
            if (dialogChannel == null || dialogData == null ||
                dialogData.DialogList == null || dialogData.DialogList.Count == 0 ||
                dialogCamera == null || fullCamera == null || dialogCamera == fullCamera ||
                equipment == null || acquiredUI == null || activePriority <= inactivePriority)
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
            dialogChannel.RaiseEvent(new StartDialogEvent().InitData(dialogData));
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
            if (!_running || _acquired || !isActiveAndEnabled) return;
            dialogChannel.RemoveListener<EndDialogEvent>(OnDialogFinished);
            _acquired = true;
            equipment.Equip();
            acquiredUI.SetActive(true);
            StartCoroutine(FinishAcquisition());
        }

        private IEnumerator FinishAcquisition()
        {
            yield return new WaitForSecondsRealtime(acquiredUIDuration);
            acquiredUI.SetActive(false);
            dialogCamera.Priority = inactivePriority;
            fullCamera.Priority = activePriority;
            _running = false;
        }

        private void RestoreCameras()
        {
            if (dialogCamera != null) dialogCamera.Priority = _previousDialogPriority;
            if (fullCamera != null) fullCamera.Priority = _previousFullPriority;
        }

        private void OnDisable()
        {
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
