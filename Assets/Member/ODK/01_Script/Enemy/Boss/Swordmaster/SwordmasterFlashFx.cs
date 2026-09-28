using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Swordmaster
{
    [DisallowMultipleComponent]
    public class SwordmasterFlashFx : MonoBehaviour
    {
        [SerializeField] private Vector2 eyeLocalOffset = new Vector2(0.15f, 1.44f);
        [SerializeField] private int eyeSortingOrder = 20;

        private Transform eyeParent;

        public int EyeSortingOrder => eyeSortingOrder;

        public Vector3 EyePosition
        {
            get
            {
                Transform root = eyeParent != null ? eyeParent : transform;
                return root.TransformPoint(eyeLocalOffset);
            }
        }

        public void Setup(Transform eyeRoot, SpriteRenderer bodyRenderer)
        {
            eyeParent = eyeRoot;
        }
    }
}
