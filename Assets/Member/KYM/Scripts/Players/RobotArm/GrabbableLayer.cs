using UnityEngine;

namespace Member.KYM.Scripts.Players.RobotArm
{
    public static class GrabbableLayer
    {
        public const string Name = "Grabbable";

        public static int Index => LayerMask.NameToLayer(Name);

        public static int Mask
        {
            get
            {
                int layerIndex = Index;
                return layerIndex >= 0 ? 1 << layerIndex : 0;
            }
        }

        public static bool TryApply(GameObject target)
        {
            int layerIndex = Index;
            if (target == null || layerIndex < 0)
                return false;

            target.layer = layerIndex;
            return true;
        }
    }
}
