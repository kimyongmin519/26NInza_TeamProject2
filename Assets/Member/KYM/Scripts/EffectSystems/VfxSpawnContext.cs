using UnityEngine;

namespace Member.KYM.Scripts.EffectSystems
{
    public readonly struct VfxSpawnContext
    {
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Color Tint { get; }

        public VfxSpawnContext(
            Vector3 position,
            Quaternion rotation,
            Color tint)
        {
            Position = position;
            Rotation = rotation;
            Tint = tint;
        }

        public static VfxSpawnContext Default(
            Vector3 position,
            Quaternion rotation)
        {
            return new VfxSpawnContext(position, rotation, Color.white);
        }
    }
}
