using System.Collections.Generic;
using Member.KYM.Scripts.Players;
using UnityEngine;
using UnityEngine.Events;

namespace Member.KYM.Scripts.CoreSystems.InteractSystems
{
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private LayerMask whatIsPlayer = ~0;
        [SerializeField] private InteractionPromptView interactionPrompt;
        [SerializeField] private string interactionText;
        [SerializeField] private float successDuration = 0.5f;
        [SerializeField] private bool interactOnce = true;

        public UnityEvent OnSuccess = new();
        public UnityEvent<PlayerController> OnPlayerSuccess = new();

        public bool IsIn => _playerColliders.Count > 0 && !_completed;
        public PlayerController CurrentPlayer => _player;
        public float Progress => successDuration <= 0f
            ? 0f
            : Mathf.Clamp01(_currentTime / successDuration);

        private readonly HashSet<Collider2D> _playerColliders = new();
        private PlayerController _player;
        private float _currentTime;
        private bool _isHolding;
        private bool _completed;
        private bool _subscribed;

        private void Awake()
        {
            if (interactionPrompt != null)
                interactionPrompt.SetInteractionText(interactionText);

            SetPromptVisible(false);
            SetProgress(0f);
        }

        private void Update()
        {
            if (!IsIn || !_isHolding)
                return;

            if (successDuration <= 0f)
            {
                CompleteInteraction();
                return;
            }

            _currentTime += Time.deltaTime;
            SetProgress(Progress);

            if (_currentTime >= successDuration)
                CompleteInteraction();
        }

        public void ResetInteraction()
        {
            _completed = false;
            ResetHold();

            if (_playerColliders.Count > 0)
            {
                SubscribeInput();
                SetPromptVisible(true);
            }
        }

        private void HandleInteractKeyPressed(bool pressed)
        {
            if (!IsIn)
                return;

            _isHolding = pressed;
            interactionPrompt?.SetHolding(pressed);

            if (!pressed)
                ResetHold();
        }

        private void CompleteInteraction()
        {
            PlayerController player = _player;
            _isHolding = false;
            interactionPrompt?.SetHolding(false);
            SetProgress(1f);

            OnSuccess?.Invoke();
            OnPlayerSuccess?.Invoke(player);

            if (interactOnce)
            {
                _completed = true;
                UnsubscribeInput();
                SetPromptVisible(false);
            }
            else
            {
                ResetHold();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_completed || !IsPlayerLayer(other.gameObject.layer))
                return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || (_player != null && _player != player))
                return;

            if (!_playerColliders.Add(other))
                return;

            _player = player;
            SubscribeInput();
            SetPromptVisible(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!_playerColliders.Remove(other) || _playerColliders.Count > 0)
                return;

            LeaveInteractionRange();
        }

        private void OnDisable()
        {
            _playerColliders.Clear();
            LeaveInteractionRange();
        }

        private void SubscribeInput()
        {
            if (_subscribed || _player == null || _player.PlayerInput == null)
                return;

            _player.PlayerInput.OnInteractKeyPressed += HandleInteractKeyPressed;
            _subscribed = true;
        }

        private void UnsubscribeInput()
        {
            if (!_subscribed)
                return;

            if (_player != null && _player.PlayerInput != null)
            {
                _player.PlayerInput.OnInteractKeyPressed -=
                    HandleInteractKeyPressed;
            }

            _subscribed = false;
        }

        private void LeaveInteractionRange()
        {
            UnsubscribeInput();
            ResetHold();
            SetPromptVisible(false);
            _player = null;
        }

        private void ResetHold()
        {
            _isHolding = false;
            interactionPrompt?.SetHolding(false);
            _currentTime = 0f;
            SetProgress(0f);
        }

        private void SetPromptVisible(bool visible)
        {
            interactionPrompt?.SetVisible(visible);
        }

        private void SetProgress(float progress)
        {
            interactionPrompt?.SetProgress(progress);
        }

        private bool IsPlayerLayer(int layer)
        {
            return (whatIsPlayer.value & (1 << layer)) != 0;
        }

        private void OnValidate()
        {
            successDuration = Mathf.Max(0f, successDuration);

            if (!Application.isPlaying && interactionPrompt != null)
            {
                interactionPrompt.SetInteractionText(interactionText);
                interactionPrompt.SetVisible(true);
            }
        }
    }
}
