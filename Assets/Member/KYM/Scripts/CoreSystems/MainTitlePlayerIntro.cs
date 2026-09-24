using KimLIb.AnimatorSystems;
using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.Agents.FSM;
using Member.KYM.Scripts.Players;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Member.KYM.Scripts.CoreSystems
{
    [DefaultExecutionOrder(100)]
    public sealed class MainTitlePlayerIntro : MonoBehaviour
    {
        [SerializeField] private AnimParamSO sleepParam;
        [SerializeField] private AnimParamSO sitParam;

        [SerializeField] private PlayerController player;

        [Header("연출 이벤트")]
        [SerializeField] private UnityEvent onSitStarted;
        [SerializeField] private UnityEvent onIntroFinished;

        private PlayerInputSO _playerInput;
        private IAnimateRenderer _renderer;
        private bool _inputLocked;
        private bool _sitStarted;
        private bool _introFinished;

        private void Awake()
        {
            if (player == null)
                player = GetComponent<PlayerController>();

            if (player == null || player.PlayerInput == null)
            {
                Debug.LogError("메인 타이틀 인트로에 PlayerController와 PlayerInputSO가 필요합니다.", this);
                enabled = false;
                return;
            }

            _playerInput = player.PlayerInput;
            _playerInput.AllInputLock(true);
            _inputLocked = true;
        }

        private void Start()
        {
            _renderer = player.GetModule<IAnimateRenderer>();
            if (_renderer == null)
            {
                Debug.LogError("플레이어의 AgentRenderer를 찾지 못했습니다.", this);
                enabled = false;
                return;
            }

            // PlayerController.Start의 IDLE 재생 이후에 타이틀 전용 애니메이션을 적용한다.
            _renderer.PlayClip(sleepParam.ParamHash);
        }

        // 버튼, 타임라인 Signal 등에서 호출: 잠에서 깨며 SIT 재생.
        public void WakeUp()
        {
            if (_renderer == null || _sitStarted || _introFinished)
                return;

            _sitStarted = true;
            _renderer.PlayClip(sitParam.ParamHash);
            onSitStarted?.Invoke();
        }

        // SIT 애니메이션 이벤트 또는 타임라인 Signal에서 호출: 조작 복구.
        public void CompleteWakeUp()
        {
            if (!_sitStarted || _introFinished)
                return;

            _introFinished = true;
            player.ChangeState(PlayerStateEnum.IDLE);
            RestoreInput();
            onIntroFinished?.Invoke();
        }

        private void OnDisable()
        {
            RestoreInput();
        }

        private void RestoreInput()
        {
            if (!_inputLocked || _playerInput == null)
                return;

            _playerInput.AllInputLock(false);
            _inputLocked = false;
        }
    }
}
