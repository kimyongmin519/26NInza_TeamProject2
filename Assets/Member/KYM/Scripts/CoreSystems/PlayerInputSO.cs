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

        public Vector2 MoveDir => new Vector2(MoveDirX, MoveDirY);
        public float MoveDirX { get; private set; }
        public float MoveDirY { get; private set; }
        public Vector2 MousePos { get; private set; }
        private Controls _controls;
        private readonly bool[] _inputLocks = new bool[(int)global::LockKey.END];

        private void OnEnable()
        {
            if (_controls == null)
            {
                _controls = new Controls();
                _controls.Player.SetCallbacks(this);
            }
            _controls.Player.Enable();

            for (int i = 0; i < _inputLocks.Length; i++)
            {
                if (_inputLocks[i])
                {
                    ApplyInputLock((global::LockKey)i, true);
                }
            }
        }

        private void OnDisable()
        {
            _controls.Player.Disable();
        }

        public void LockInput(global::LockKey lockKey, bool isLocked)
        {
            if ((int)lockKey < 0 || lockKey >= global::LockKey.END)
            {
                return;
            }

            _inputLocks[(int)lockKey] = isLocked;
            ApplyInputLock(lockKey, isLocked);
        }

        public void AllInputLock(bool isLocked)
        {
            for (int i = 0; i < (int)global::LockKey.END; i++)
            {
                LockInput((global::LockKey)i, isLocked);
            }
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            if (IsInputLocked(global::LockKey.MOVE))
            {
                MoveDirX = 0f;
                MoveDirY = 0f;
                return;
            }

            Vector2 moveDirection = context.ReadValue<Vector2>();
            MoveDirX = moveDirection.x;
            MoveDirY = moveDirection.y;
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (IsInputLocked(global::LockKey.ATTACK))
            {
                return;
            }

            if (context.performed)
                AttackPressed?.Invoke();
        }

        public void OnAttackCancle(InputAction.CallbackContext context)
        {
            if (IsInputLocked(global::LockKey.ATTACK))
            {
                return;
            }

            if (context.performed)
                AttackCancelPressed?.Invoke();
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (IsInputLocked(global::LockKey.INTERACT))
            {
                return;
            }

            if (context.started)
                OnInteractKeyPressed?.Invoke(true);

            if (context.canceled)
                OnInteractKeyPressed?.Invoke(false);
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (IsInputLocked(global::LockKey.JUMP))
            {
                return;
            }

            if (context.performed)
                OnJumpKeyPressed?.Invoke();
        }

        public void OnDash(InputAction.CallbackContext context)
        {
            if (IsInputLocked(global::LockKey.DASH))
            {
                return;
            }

            if (context.performed)
                OnDashKeyPressed?.Invoke();
        }

        public void OnMousePos(InputAction.CallbackContext context)
        {
            if (IsInputLocked(global::LockKey.MOUSEPOS))
            {
                return;
            }

            MousePos = context.ReadValue<Vector2>();
        }

        public void OnTakeOut(InputAction.CallbackContext context)
        {
            if (IsInputLocked(global::LockKey.ATTACK))
            {
                return;
            }

            if (context.performed)
                OnTakeOutKeyPressed?.Invoke();
        }

        private bool IsInputLocked(global::LockKey lockKey)
        {
            return (int)lockKey >= 0 && lockKey < global::LockKey.END && _inputLocks[(int)lockKey];
        }

        private void ApplyInputLock(global::LockKey lockKey, bool isLocked)
        {
            if (_controls == null)
            {
                return;
            }

            switch (lockKey)
            {
                case global::LockKey.MOVE:
                    SetActionLock(_controls.Player.Move, isLocked);
                    MoveDirX = 0f;
                    MoveDirY = 0f;
                    break;
                case global::LockKey.ATTACK:
                    SetActionLock(_controls.Player.Attack, isLocked);
                    SetActionLock(_controls.Player.AttackCancle, isLocked);
                    SetActionLock(_controls.Player.TakeOut, isLocked);
                    break;
                case global::LockKey.INTERACT:
                    SetActionLock(_controls.Player.Interact, isLocked);
                    break;
                case global::LockKey.JUMP:
                    SetActionLock(_controls.Player.Jump, isLocked);
                    break;
                case global::LockKey.DASH:
                    SetActionLock(_controls.Player.Dash, isLocked);
                    break;
                case global::LockKey.MOUSEPOS:
                    SetActionLock(_controls.Player.MousePos, isLocked);
                    break;
            }
        }

        private void SetActionLock(InputAction action, bool isLocked)
        {
            if (isLocked)
            {
                action.Disable();
            }
            else
            {
                action.Enable();
            }
        }
    }
}
