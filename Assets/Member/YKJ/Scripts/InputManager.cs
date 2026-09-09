using Core;
using KimLIb.EventSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum LockKey
{
    MOVE, ATTACK, INTERACT, JUMP, DASH, MOUSEPOS, END
}
public class InputManager : MonoBehaviour
{
    [SerializeField] private PlayerInputSO PlayerInput;
    [SerializeField] private EventChannelSO InputChannel;

    private void Start()
    {
        InputChannel.AddListener<LockInputEvent>(HandleLockEvent);
        InputChannel.AddListener<LockInputAllEvent>(HandleAllLockEvent);
        //이벤트 버스 구독하기
    }

    private void OnDestroy()
    {
        InputChannel.RemoveListener<LockInputEvent>(HandleLockEvent);
        InputChannel.RemoveListener<LockInputAllEvent>(HandleAllLockEvent);
    }
    private void HandleLockEvent(LockInputEvent evt)
    {
        PlayerInput.LockInput(evt.LockKey, evt.IsLocked);
    }

    private void HandleAllLockEvent(LockInputAllEvent evt)
    {
        PlayerInput.AllInputLock(evt.IsLocked);
    }
}
