using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Member.KYM.Scripts.Enemies.Boss.BT.Channels
{
#if UNITY_EDITOR
    [CreateAssetMenu(menuName = "Behavior/Event Channels/StateChannel")]
#endif
    [Serializable, GeneratePropertyBag]
    [EventChannelDescription(name: "StateChannel", message: "Change [StateEnum]", category: "Events", id: "14cbace108d85922b080dd43fd0c413e")]
    public sealed partial class StateChannel : EventChannel<BossStateEnum> { }
}

