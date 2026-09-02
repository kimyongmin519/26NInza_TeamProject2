using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core
{
    [CreateAssetMenu(fileName = "PlayerInputSO", menuName = "KImSO/Core/PlayerInputSO", order = 0)]
    public class PlayerInputSO : ScriptableObject, Controls.IPlayerActions
    {
        public event Action AttackPressed;
        public event Action InteractPressed;

        public Vector2 MoveDir { get; private set; }
        public float MoveDirX => MoveDir.x;
        public Vector2 MousePos { get; private set; }
        private Controls _controls;

        private Dictionary<LockKey, bool> LockKeyDic = new Dictionary<LockKey, bool>();

        private void OnEnable()
        {
            InitDic();
            if (_controls == null)
            {
                _controls = new Controls();
                _controls.Player.SetCallbacks(this);
            }
            _controls.Player.Enable();
        }

        private void InitDic()
        {
            LockKeyDic.Clear();
            foreach (LockKey key in Enum.GetValues(typeof(LockKey)))
                LockKeyDic[key] = false;
        }
        private void OnDisable()
        {
            _controls.Player.Disable();
        }

        public void OnMove(InputAction.CallbackContext context)
        {

            if (IsLocked(LockKey.MOVE))
            {
                MoveDir = Vector2.zero;
                return;
            }

            MoveDir = context.ReadValue<Vector2>();
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (IsLocked(LockKey.ATTACK))
                return;
            if (context.performed)
                AttackPressed?.Invoke();
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (IsLocked(LockKey.INTERACT))
                return;
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

        public void LockInput(LockKey lockEnum, bool value)
        {
            if (LockKeyDic.ContainsKey(lockEnum))
                LockKeyDic[lockEnum] = value;
            else
                Debug.LogError($"LockKey {lockEnum} 가 딕셔너리에 존재하지 않습니다");
        }

        public void AllInputLock(bool value)
        {
            for (int i = 0; i < (int)LockKey.END; i++)
                LockKeyDic[(LockKey)i] = value;
        }
        private bool IsLocked(LockKey key) => LockKeyDic.TryGetValue(key, out bool locked) && locked;
    }
}
