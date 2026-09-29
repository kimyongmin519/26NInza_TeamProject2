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
        [SerializeField] private CinemachineCamera introCamera;
        [SerializeField] private CinemachineCamera gameplayCamera;
        [SerializeField] private int activePriority = 20;
        [SerializeField] private int inactivePriority;

        private PlayerInputSO _playerInput;
        private bool _waitingForDialogue;

        private IEnumerator Start()
        {
            _playerInput = GetComponent<PlayerController>()?.PlayerInput;
            if (_playerInput != null)
                _playerInput.AllInputLock(true);

            if (introCamera != null && gameplayCamera != null && introCamera != gameplayCamera)
            {
                introCamera.Priority = activePriority;
                gameplayCamera.Priority = inactivePriority;
            }

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

            _waitingForDialogue = true;
            var request = new StartBubbleDialogEvent().InitData(dialogData, transform, FinishIntro);
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
            if (introCamera != null && gameplayCamera != null && introCamera != gameplayCamera)
            {
                introCamera.Priority = inactivePriority;
                gameplayCamera.Priority = activePriority;
            }

            _playerInput?.AllInputLock(false);
            _playerInput = null;
        }

        private void OnDisable() => FinishIntro();
    }
}
