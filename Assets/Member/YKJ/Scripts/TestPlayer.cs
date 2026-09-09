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
    [SerializeField] private AgentRenderer agentRenderer;
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
        if (playerInput.MoveDir == Vector2.zero)
        {
            agentRenderer.PlayClip(Animator.StringToHash("IDLE"));
        }
        else
        {
            agentRenderer.PlayClip(Animator.StringToHash("MOVE"));
        }
        playerMover.SetMovementX(playerInput.MoveDir);
    }
}
