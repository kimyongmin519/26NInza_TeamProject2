using System;
using Member.KYM.Scripts.Agents;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Member.KYM.Scripts.Enemies.Boss.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "MoveToDirection", story: "[Enemy] go to [Direction] in [Duration]", category: "Action", id: "4d992756f739b8594aee31441dd63b4b")]
    public partial class MoveToDirectionAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractBoss> Enemy;
        [SerializeReference] public BlackboardVariable<float> Direction;
        [SerializeReference] public BlackboardVariable<float> Duration;

        private IMover _mover;
        private float _startTime;
        private bool _isGround;
        
        protected override Status OnStart()
        {
            if (Enemy.Value == null || Enemy.Value.Mover == null || Enemy.Value.Renderer == null
                || Enemy.Value.Sensor == null) //Enemy.Value.AttackConfig == null
                return Status.Failure;
            
            float direction = Enemy.Value.Renderer.FacingDirection;
            
            _mover = Enemy.Value.Mover;
            _mover.SetMovementX(direction);
            _startTime = Time.time;
            _isGround = true;
            _mover.OnGroundStatusChange += HandleGroundStatusChange;
            
            return Status.Running;
        }
        //주어진 시간만큼 걸어가는 동안 앞에 길이 끊어졌다면 실패이고, 주어진 시간내내 걸어갔다면 성공이다.
        protected override Status OnUpdate()
        {
            if(!_isGround)
                return Status.Failure;

            Vector2 moveDirection = Enemy.Value.transform.right;
            float distance = Enemy.Value.Sensor.BoxCastObstacle(moveDirection, 10f, out RaycastHit2D hit);

            /*if (distance < Enemy.Value.AttackConfig.StoppingDistance)
                return Status.Failure;*/

            if (Time.time - _startTime >= Duration.Value)
            {
                return Status.Success;    
            }

            return Status.Running;
        }

        protected override void OnEnd()
        {
            if(_mover != null)
                _mover.OnGroundStatusChange -= HandleGroundStatusChange;
        }

        private void HandleGroundStatusChange(bool isGround) => _isGround = isGround;
    }
}

