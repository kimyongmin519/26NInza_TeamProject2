using System;
using KimLIb.AnimatorSystems;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Member.KYM.Scripts.Enemies.Boss.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "PlayClipAction", story: "[Enemy] play [AnimationClip]", category: "Action", id: "549698b3d6667064ab38beb9510691ca")]
    public partial class PlayClipAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractBoss> Enemy;
        [SerializeReference] public BlackboardVariable<AnimParamSO> AnimationClip;

        protected override Status OnStart()
        {
            if (Enemy.Value == null || AnimationClip.Value == null || Enemy.Value.Renderer == null)
                return Status.Failure;
            
            Enemy.Value.Renderer.PlayClip(AnimationClip.Value.ParamHash);
            
            return Status.Success;
        }
    }
}

