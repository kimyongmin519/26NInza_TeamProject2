using System;
using KimLIb.AnimatorSystems;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Member.KYM.Scripts.Enemies.Boss.BT.Channels
{
#if UNITY_EDITOR
    [CreateAssetMenu(menuName = "Behavior/Event Channels/AnimationChannel")]
#endif
    [Serializable, GeneratePropertyBag]
    [EventChannelDescription(name: "AnimationChannel", message: "Play [AnimationClip]", category: "Events", id: "d2d89ec1ad8612bc9ee0705a45f1037c")]
    public sealed partial class AnimationChannel : EventChannel<AnimParamSO> { }
}

