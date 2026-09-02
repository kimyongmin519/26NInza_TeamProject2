using Core;
using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.Players;
using System;
using UnityEngine;

public class TestPlayer : Agent
{
    [SerializeField] PlayerInputSO playerInput;

    [SerializeField] private MapPlayerMover playerMover;
    [SerializeField] private AgentSensor agentSensor;
    protected override void Awake()
    {
        base.Awake();
        playerMover.Initialize(this);
        playerInput.InteractPressed += HandleInteract;
    }

    private void HandleInteract()
    {
        Debug.Log("이벤트 호출");
        agentSensor.IsInteractableInDirection();
    }

    private void OnDestroy()
    {
        playerInput.InteractPressed -= HandleInteract;
    }
    private void Update()
    {
        playerMover.SetMovementX(playerInput.MoveDir);
    }
}
