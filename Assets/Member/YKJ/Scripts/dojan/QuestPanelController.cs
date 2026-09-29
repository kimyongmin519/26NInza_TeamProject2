using UnityEngine;
using UnityEngine.InputSystem;
using Member.KYM.Scripts.CoreSystems;

public sealed class QuestPanelController : MonoBehaviour
{
    [SerializeField] private GameObject questUI;
    [SerializeField] private PlayerInputSO playerInput;
    private bool _ownsInputLock;
    private bool _previousMoveLock;
    private bool _previousInteractLock;

    private void Awake() => Close();

    private void Update()
    {
        if (_ownsInputLock && (questUI == null || !questUI.activeInHierarchy))
            RestoreInput();
        if (questUI != null && questUI.activeSelf &&
            Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    public void Open()
    {
        if (questUI == null) return;
        if (!_ownsInputLock && playerInput != null)
        {
            _previousMoveLock = playerInput.IsInputLocked(LockKey.MOVE);
            _previousInteractLock = playerInput.IsInputLocked(LockKey.INTERACT);
            _ownsInputLock = true;
            playerInput.LockInput(LockKey.MOVE, true);
            playerInput.LockInput(LockKey.INTERACT, true);
        }
        questUI.SetActive(true);
    }

    public void Close()
    {
        if (questUI != null) questUI.SetActive(false);
        RestoreInput();
    }

    private void OnDisable() => Close();

    private void RestoreInput()
    {
        if (!_ownsInputLock) return;
        _ownsInputLock = false;
        if (playerInput == null) return;
        playerInput.LockInput(LockKey.MOVE, _previousMoveLock);
        playerInput.LockInput(LockKey.INTERACT, _previousInteractLock);
    }
}
