using System.Collections;
using KimLIb.EventSystem;
using Member.KYM.Scripts.Players;
using Unity.Cinemachine;
using UnityEngine;

namespace Member.KYM.Scripts.CoreSystems
{
    public sealed class TutorialSceneEntryMonologue : MonoBehaviour
    {
        [SerializeField] private EventChannelSO bubbleDialogChannel;
        [SerializeField] private DialogDataSO dialogData;
        [SerializeField] private Transform bubbleAnchor;
        [SerializeField] private CinemachineCamera introCamera;
        [SerializeField] private CinemachineCamera gameplayCamera;
        [SerializeField] private int activePriority = 20;
        [SerializeField] private int inactivePriority;
        [SerializeField, Min(0f)] private float dialogueDelay;

        private PlayerInputSO _playerInput;
        private bool _waitingForDialogue;

        private IEnumerator Start()
        {
            _playerInput = GetComponent<PlayerController>()?.PlayerInput;
            if (_playerInput != null)
                _playerInput.AllInputLock(true);

            SetDialogueCameraActive(false);

            // 씬 전환 화면이 열린 뒤 플레이어 위에 말풍선을 표시한다.
            yield return null;
            while (TransitionUIController.IsTransitioning ||
                   DialogManager.Talking || BubbleDialogManager.Talking)
                yield return null;

            if (bubbleDialogChannel == null || dialogData == null)
            {
                Debug.LogWarning("튜토리얼 입장 말풍선의 채널 또는 대화 데이터가 연결되지 않았습니다.", this);
                FinishIntro();
                yield break;
            }

            SetDialogueCameraActive(true);
            CinemachineBrain brain = Camera.main?.GetComponent<CinemachineBrain>();

            // 카메라가 새 우선순위를 반영한 뒤 블렌딩을 마칠 때까지 기다린다.
            yield return null;
            while (brain != null && brain.IsBlending)
                yield return null;

            if (dialogueDelay > 0f)
                yield return new WaitForSecondsRealtime(dialogueDelay);

            _waitingForDialogue = true;
            var request = new StartBubbleDialogEvent().InitData(
                dialogData, bubbleAnchor != null ? bubbleAnchor : transform, FinishIntro);
            bubbleDialogChannel.RaiseEvent(request);
            if (!request.Accepted)
            {
                Debug.LogWarning("튜토리얼 입장 말풍선을 시작하지 못했습니다. BubbleDialogManager를 확인해주세요.", this);
                FinishIntro();
            }
        }

        private void FinishIntro()
        {
            if (!_waitingForDialogue && _playerInput == null)
                return;

            _waitingForDialogue = false;
            SetDialogueCameraActive(false);

            _playerInput?.AllInputLock(false);
            _playerInput = null;
        }

        private void SetDialogueCameraActive(bool active)
        {
            if (introCamera == null || gameplayCamera == null || introCamera == gameplayCamera)
                return;

            introCamera.Priority = active ? activePriority : inactivePriority;
            gameplayCamera.Priority = active ? inactivePriority : activePriority;
        }

        private void OnDisable() => FinishIntro();
    }
}
