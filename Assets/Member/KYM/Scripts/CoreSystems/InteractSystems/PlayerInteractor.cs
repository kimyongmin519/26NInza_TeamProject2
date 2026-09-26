using System.Collections.Generic;
using Member.KYM.Scripts.Players;
using UnityEngine;
using UnityEngine.Events;

namespace Member.KYM.Scripts.CoreSystems.InteractSystems
{
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private LayerMask whatIsPlayer;

        [Header("E 버튼 표시")]
        [SerializeField] private SpriteRenderer promptRenderer;
        [SerializeField] private Sprite promptSprite;
        [SerializeField] private Sprite pressedSprite;

        public UnityEvent OnSuccess = new();

        private readonly HashSet<Collider2D> _playerColliders = new();
        private PlayerController _player;
        private PlayerInputSO _input;
        private bool _isPressed;

        private void Awake()
        {
            HidePrompt();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if ((whatIsPlayer.value & (1 << other.gameObject.layer)) == 0)
                return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || player.PlayerInput == null ||
                (_player != null && _player != player))
                return;

            if (!_playerColliders.Add(other) || _playerColliders.Count > 1)
                return;

            _player = player;
            _input = player.PlayerInput;
            _input.OnInteractKeyPressed += HandleInteract;
            _isPressed = false;
            if (promptRenderer != null)
            {
                promptRenderer.sprite = promptSprite;
                promptRenderer.enabled = true;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (_playerColliders.Remove(other) && _playerColliders.Count == 0)
                ClearPlayer();
        }

        private void HandleInteract(bool pressed)
        {
            if (_isPressed == pressed)
                return;

            _isPressed = pressed;
            if (promptRenderer != null)
                promptRenderer.sprite = pressed ? pressedSprite : promptSprite;

            if (pressed)
                OnSuccess.Invoke();
        }

        private void OnDisable()
        {
            ClearPlayer();
        }

        private void ClearPlayer()
        {
            if (_input != null)
                _input.OnInteractKeyPressed -= HandleInteract;

            _input = null;
            _player = null;
            _isPressed = false;
            _playerColliders.Clear();
            HidePrompt();
        }

        private void HidePrompt()
        {
            if (promptRenderer != null)
                promptRenderer.enabled = false;
        }
    }
}
