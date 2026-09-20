using GGMLib.ObjectPool.Runtime;
using KimLIb.EventSystem;
using Member.KYM.Scripts.EffectSystems;
using UnityEngine;

namespace Member.KYM.Scripts.CoreSystems.Events
{
    public static class CreateEvents
    {
        public static readonly ShowPoolingEffect ShowPoolingEffect = new();
    }

    public class ShowPoolingEffect : GameEvent
    {
        public PoolItemSO ItemData { get; private set; }
        public VfxSpawnContext Context { get; private set; }
        public Vector3 Position => Context.Position;
        public Quaternion Rotation => Context.Rotation;

        public ShowPoolingEffect InitData(
            PoolItemSO itemData,
            VfxSpawnContext context)
        {
            ItemData = itemData;
            Context = context;
            return this;
        }

        public ShowPoolingEffect InitData(
            PoolItemSO itemData,
            Vector3 position,
            Quaternion rotation)
        {
            return InitData(
                itemData,
                VfxSpawnContext.Default(position, rotation));
        }
    }
}
