using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core
{
    [CreateAssetMenu(fileName = "PlayerInputSO", menuName = "KImSO/Core/PlayerInputSO", order = 0)]
    public class PlayerInputSO : ScriptableObject, Controls.IPlayerActions
    {
        public event Action AttackPressed;
        public event Action InteractPressed;

        public float MoveDirX { get; private set; }
        public Vector2 MousePos { get; private set; }
        private Controls _controls;

        private void OnEnable()
        {
            if (_controls == null)
            {
                _controls = new Controls();
                _controls.Player.SetCallbacks(this);
            }
            _controls.Player.Enable();
        }

        private void OnDisable()
        {
            _controls.Player.Disable();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            MoveDirX = context.ReadValue<Vector2>().x;
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (context.performed)
                AttackPressed?.Invoke();
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (context.performed)
                InteractPressed?.Invoke();
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            
        }

        public void OnDash(InputAction.CallbackContext context)
        {
            
        }

        public void OnMousePos(InputAction.CallbackContext context)
        {
            MousePos = context.ReadValue<Vector2>();
        }
    }
}
