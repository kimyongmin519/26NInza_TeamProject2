using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Member.KYM.Scripts.Enemies.Boss.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "EnemyFlipToTarget", story: "Flip [Enemy] to [TargetGameObject]", category: "Action", id: "142ec3836ad7b7233dbb3abb89fee188")]
    public partial class EnemyFlipToTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractBoss> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;
        //타겟이 없으면 반대로 플립
        protected override Status OnStart()
        {
            if(Enemy.Value == null || Enemy.Value.Renderer == null)
                return Status.Failure;

            if (TargetGameObject.Value == null)
            {
                Enemy.Value.Renderer.FlipController(Enemy.Value.Renderer.FacingDirection * -1f);
            }
            else
            {
                Vector2 direction = TargetGameObject.Value.transform.position - Enemy.Value.transform.position;
                Enemy.Value.Renderer.FlipController(Mathf.Sign(direction.x));
            }
            
            return Status.Success;
        }
    }
}

