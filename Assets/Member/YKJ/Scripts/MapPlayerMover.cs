using KimLIb.ModuleSystems;
using Member.KYM.Scripts.Agents;
using System;
using UnityEngine;

public class MapPlayerMover : MonoBehaviour
{
    [SerializeField] private float moveSpeed;
    public Rigidbody2D RigidBody2D { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool CanManualMovement { get; set; } = true;
    public event Action<bool> OnGroundStatusChange;
    public event Action<Vector2> OnVelocityChange;

    private Vector2 _moveDir;
    private ModuleOwner _owner;

    public void Initialize(ModuleOwner owner)
    {
        _owner = owner;
        RigidBody2D = owner.GetComponent<Rigidbody2D>();
    }

    public void SetMoveSpeedMultiplier(float value)
    {

    }

    public void SetGravityScale(float value)
    {

    }

    public void AddForceToAgent(Vector2 force)
    {

    }

    private void FixedUpdate()
    {
        RigidBody2D.linearVelocity = _moveDir * moveSpeed;
    }

    public void StopImmediately(bool xAxis, bool yAxis)
    {
        if (xAxis)
            RigidBody2D.linearVelocityX = 0;
        if (yAxis)
            RigidBody2D.linearVelocityY = 0;
    }

    public void SetMovementX(Vector2 value) => _moveDir = value;
}
