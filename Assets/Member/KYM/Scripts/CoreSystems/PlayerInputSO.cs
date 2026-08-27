using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Member.KYM.Scripts.CoreSystems
{
    [CreateAssetMenu(fileName = "PlayerInputSO", menuName = "KimSO/Core/PlayerInputSO", order = 0)]
    public class PlayerInputSO : ScriptableObject, Controls.IPlayerActions
    {
        public event Action AttackPressed;
        public event Action AttackCancelPressed;
        public event Action OnJumpKeyPressed;
        public event Action OnDashKeyPressed;
        public event Action<bool> OnInteractKeyPressed;
        public event Action OnTakeOutKeyPressed;

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

        public void OnAttackCancle(InputAction.CallbackContext context)
        {
            if (context.performed)
                AttackCancelPressed?.Invoke();
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (context.started)
                OnInteractKeyPressed?.Invoke(true);

            if (context.canceled)
                OnInteractKeyPressed?.Invoke(false);
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnJumpKeyPressed?.Invoke();
        }

        public void OnDash(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnDashKeyPressed?.Invoke();
        }

        public void OnMousePos(InputAction.CallbackContext context)
        {
            MousePos = context.ReadValue<Vector2>();
        }

        public void OnTakeOut(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnTakeOutKeyPressed?.Invoke();
        }
    }
}
