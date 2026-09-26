using Member.KYM.Scripts.Players;
using Member.KYM.Scripts.UI;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace Member.KYM.Scripts.Enemies.Boss
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayableDirector))]
    public sealed class BossStageIntroController : MonoBehaviour
    {
        [Header("전투 대상")]
        [SerializeField] private BTAgentBoss boss;
        [SerializeField] private GameObject playerObject;

        [Header("인트로 스킵")]
        [SerializeField] private Key skipKey = Key.Escape;
        [SerializeField] private CinematicViewUI cinematicView;

        [Header("인트로 스킵 시 전투 시작 위치")]
        [SerializeField] private Vector3 bossCombatPosition;
        [SerializeField] private Vector3 playerCombatPosition;

        [Header("전투 시작 시 공통 연출")]
        [SerializeField] private UnityEvent onCombatStarted;

        private static string _retryScenePath;
        private static bool _hasCombatPose;
        private static Vector3 _bossStartPosition;
        private static Quaternion _bossStartRotation;
        private static Vector3 _playerStartPosition;
        private static Quaternion _playerStartRotation;

        private PlayableDirector _director;
        private BehaviorGraphAgent _behavior;
        private PlayerController _player;
        private bool _skipIntro;
        private bool _introStarted;
        private bool _combatStarted;
        private bool _playerDied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRetryState()
        {
            _retryScenePath = null;
            _hasCombatPose = false;
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        }

        private static void HandleActiveSceneChanged(Scene previous, Scene next)
        {
            if (_retryScenePath != null && next.path != _retryScenePath)
            {
                _retryScenePath = null;
                _hasCombatPose = false;
            }
        }

        private void Awake()
        {
            _director = GetComponent<PlayableDirector>();
            _player = playerObject != null ? playerObject.GetComponent<PlayerController>() : null;
            _behavior = boss != null ? boss.GetComponent<BehaviorGraphAgent>() : null;

            if (_player == null || _behavior == null)
            {
                Debug.LogError("보스 인트로에 플레이어와 보스 BT가 필요합니다.", this);
                enabled = false;
                return;
            }

            // 그래프는 비활성화해도 Awake에서 초기화된다. Update의 자동 실행만 막는다.
            _behavior.enabled = false;
            _skipIntro = _retryScenePath == gameObject.scene.path;
            if (_skipIntro)
                _retryScenePath = null;
        }

        private void OnEnable()
        {
            if (_director != null)
                _director.stopped += HandleIntroStopped;
            if (_player != null)
                _player.OnDeath.AddListener(HandlePlayerDeath);
        }

        private void Start()
        {
            if (_behavior == null)
                return;

            if (_skipIntro)
            {
                MoveToCombatStart();
                RestoreIntroPresentation();
                StartCombat();
                return;
            }

            if (_director.playableAsset == null)
            {
                Debug.LogError("보스 인트로 타임라인이 할당되지 않았습니다.", this);
                StartCombat();
                return;
            }

            _director.time = 0;
            _introStarted = true;
            _director.Play();
        }

        private void Update()
        {
            Key activeSkipKey = skipKey == Key.None ? Key.Escape : skipKey;
            if (_introStarted && !_combatStarted && !_playerDied &&
                Keyboard.current != null && Keyboard.current[activeSkipKey].wasPressedThisFrame)
            {
                SkipIntro();
                return;
            }

            // Hold 모드는 마지막 프레임을 유지하므로 stopped 이벤트만으로는 종료를 보장할 수 없다.
            if (_introStarted && !_combatStarted && !_playerDied &&
                _director.time >= _director.duration - 0.001d)
            {
                StartCombat();
            }
        }

        private void OnDisable()
        {
            if (_director != null)
                _director.stopped -= HandleIntroStopped;
            if (_player != null)
                _player.OnDeath.RemoveListener(HandlePlayerDeath);
        }

        private void HandleIntroStopped(PlayableDirector stoppedDirector)
        {
            if (_introStarted && stoppedDirector == _director && !_playerDied &&
                stoppedDirector.time >= stoppedDirector.duration - 0.001d)
                StartCombat();
        }

        private void HandlePlayerDeath()
        {
            _playerDied = true;
            _retryScenePath = gameObject.scene.path;

            if (_director.state == PlayState.Playing)
                _director.Stop();
            _behavior.enabled = false;
        }

        // 버튼의 OnClick에서도 호출할 수 있다. 잠긴 PlayerInputSO와 무관하게 Escape로도 호출된다.
        public void SkipIntro()
        {
            // 대화 시스템은 별도로 종료해야 하므로 대화 도중에는 인트로만 건너뛰지 않는다.
            if (!_introStarted || _combatStarted || _playerDied || DialogManager.Talking)
                return;

            _introStarted = false;

            // 타임라인의 마지막 포즈와 전투 카메라를 적용한다. 중간 신호는 건너뛰므로
            // 입력 잠금과 시네마틱 바는 별도로 원상 복구한다.
            _director.time = System.Math.Max(0d, _director.duration - 0.0001d);
            _director.Evaluate();
            RestoreIntroPresentation();
            StartCombat();
        }

        private void RestoreIntroPresentation()
        {
            _player.PlayerInput?.AllInputLock(false);
            cinematicView?.HideImmediately();
        }

        private void StartCombat()
        {
            if (_combatStarted || _playerDied)
                return;

            _combatStarted = true;
            if (!_skipIntro)
                RememberCombatPose();
            _behavior.enabled = true;
            onCombatStarted?.Invoke();
        }

        private void MoveToCombatStart()
        {
            if (_hasCombatPose)
            {
                boss.transform.SetPositionAndRotation(
                    _bossStartPosition, _bossStartRotation);
                _player.transform.SetPositionAndRotation(
                    _playerStartPosition, _playerStartRotation);
                return;
            }

            boss.transform.position = bossCombatPosition;
            _player.transform.position = playerCombatPosition;
        }

        private void RememberCombatPose()
        {
            _bossStartPosition = boss.transform.position;
            _bossStartRotation = boss.transform.rotation;
            _playerStartPosition = _player.transform.position;
            _playerStartRotation = _player.transform.rotation;
            _hasCombatPose = true;
        }
    }
}
