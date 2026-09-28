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

        // 검사만 수행한다. 레이어는 에디터에서 사용자가 직접 설정한다.
        public static void Validate(GameObject target)
        {
            if (target == null)
                return;

            int layerIndex = Index;
            if (layerIndex < 0)
            {
                Debug.LogError($"프로젝트에 {Name} 레이어가 없습니다.", target);
                return;
            }

            if (target.layer != layerIndex)
                Debug.LogWarning($"{target.name}은 {Name} 레이어가 아니므로 잡기/그래플의 레이어 감지 대상에서 제외됩니다. 설정한 레이어는 유지됩니다.", target);
        }
    }
}
