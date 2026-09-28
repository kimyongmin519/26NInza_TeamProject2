using UnityEngine;

namespace Member.ODK.Scripts.Enemys.Volcanus.Legacy
{
    public class VolcanusOriginalVisualRig : MonoBehaviour
    {
        [Header("Original Golem Visual")]
        [SerializeField] private Transform visualRoot;

        [Header("Volcanus Motion Controls")]
        [SerializeField] private Transform headControl;
        [SerializeField] private Transform leftArmControl;
        [SerializeField] private Transform rightArmControl;

        private Transform headBone;
        private Transform leftArmBone;
        private Transform rightArmBone;
        private Transform fistBone;
        private Transform sawBone;
        private Transform bodySprite;
        private Transform neckSprite;
        private PieceBinding headBinding;
        private PieceBinding leftArmBinding;
        private PieceBinding rightArmBinding;
        private bool initialized;

        public bool IsActive => initialized && isActiveAndEnabled && visualRoot != null && visualRoot.gameObject.activeInHierarchy;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (initialized || visualRoot == null) return;

            Animator sourceAnimator = visualRoot.GetComponentInChildren<Animator>(true);
            if (sourceAnimator != null)
            {
                sourceAnimator.Rebind();
                sourceAnimator.Update(0f);
                sourceAnimator.enabled = false;
            }

            foreach (MonoBehaviour behaviour in visualRoot.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null || behaviour == this) continue;
                string typeName = behaviour.GetType().Name;
                if (typeName == "UnitControl" || typeName == "AnimationEvent")
                    behaviour.enabled = false;
            }

            foreach (Collider2D visualCollider in visualRoot.GetComponentsInChildren<Collider2D>(true))
                visualCollider.enabled = false;

            headBone = Find(visualRoot, "head_1_000");
            leftArmBone = Find(visualRoot, "arm_l_1_000");
            rightArmBone = Find(visualRoot, "arm_r_1_000");
            fistBone = Find(visualRoot, "arm_r_3_000");
            sawBone = Find(visualRoot, "arm_l_3_000");
            Transform bodyBone = Find(visualRoot, "body_000");
            bodySprite = bodyBone != null ? FindDirectChild(bodyBone, "body") : null;
            Transform neckBone = Find(visualRoot, "neck_000");
            neckSprite = neckBone != null ? FindDirectChild(neckBone, "neck") : null;

            headBinding = CreateBinding(headControl, headBone);
            leftArmBinding = CreateBinding(leftArmControl, leftArmBone);
            rightArmBinding = CreateBinding(rightArmControl, rightArmBone);
            initialized = true;
        }

        public void ApplyGiantScale(float bodyScale, float headScale)
        {
            Initialize();
            float safeBody = Mathf.Max(0.1f, bodyScale);
            float safeHead = Mathf.Max(0.1f, headScale);
            if (bodySprite != null) bodySprite.localScale *= safeBody;
            if (neckSprite != null) neckSprite.localScale *= Mathf.Lerp(1f, safeBody, 0.7f);
            if (headBinding.IsValid)
            {
                headBinding.BoneScale *= safeHead;
                headBinding.Bone.localScale = headBinding.BoneScale;
            }
        }

        public bool TryGetBounds(Volcanus.StrikePart part, out Bounds bounds)
        {
            Initialize();
            Transform target = part switch
            {
                Volcanus.StrikePart.Fist => fistBone,
                Volcanus.StrikePart.Saw => sawBone,
                Volcanus.StrikePart.Head => headBone,
                _ => bodySprite
            };
            return Volcanus.TryGetRendererBounds(target, false, out bounds);
        }

        private void LateUpdate()
        {
            Apply(headBinding);
            Apply(leftArmBinding);
            Apply(rightArmBinding);
        }

        private PieceBinding CreateBinding(Transform control, Transform bone)
        {
            if (control == null || bone == null || visualRoot == null) return default;
            return new PieceBinding
            {
                IsValid = true,
                Control = control,
                Bone = bone,
                ControlPosition = transform.InverseTransformPoint(control.position),
                ControlRotation = Quaternion.Inverse(transform.rotation) * control.rotation,
                ControlScale = control.lossyScale,
                BonePosition = visualRoot.InverseTransformPoint(bone.position),
                BoneRotation = Quaternion.Inverse(visualRoot.rotation) * bone.rotation,
                BoneScale = bone.localScale
            };
        }

        public void RebaseControlScale()
        {
            if (headBinding.IsValid) headBinding.ControlScale = headBinding.Control.lossyScale;
            if (leftArmBinding.IsValid) leftArmBinding.ControlScale = leftArmBinding.Control.lossyScale;
            if (rightArmBinding.IsValid) rightArmBinding.ControlScale = rightArmBinding.Control.lossyScale;
        }

        private void Apply(PieceBinding binding)
        {
            if (!binding.IsValid || binding.Control == null || binding.Bone == null) return;

            Vector3 controlPosition = transform.InverseTransformPoint(binding.Control.position);
            Vector3 positionDelta = controlPosition - binding.ControlPosition;
            Quaternion controlRotation = Quaternion.Inverse(transform.rotation) * binding.Control.rotation;
            Quaternion rotationDelta = controlRotation * Quaternion.Inverse(binding.ControlRotation);

            Vector3 targetPosition = visualRoot.TransformPoint(binding.BonePosition) +
                                     transform.TransformVector(positionDelta);
            Quaternion targetRotation = rotationDelta * visualRoot.rotation * binding.BoneRotation;
            binding.Bone.SetPositionAndRotation(targetPosition, targetRotation);

            Vector3 currentScale = binding.Control.lossyScale;
            binding.Bone.localScale = new Vector3(
                binding.BoneScale.x * SafeRatio(currentScale.x, binding.ControlScale.x),
                binding.BoneScale.y * SafeRatio(currentScale.y, binding.ControlScale.y),
                binding.BoneScale.z
            );
        }

        private static Transform Find(Transform root, string targetName)
        {
            if (root == null) return null;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == targetName) return child;
            return null;
        }

        private static Transform FindDirectChild(Transform parent, string targetName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == targetName) return child;
            }
            return null;
        }

        private static float SafeRatio(float value, float origin)
        {
            return Mathf.Abs(origin) > 0.0001f ? value / origin : 1f;
        }

        private struct PieceBinding
        {
            public bool IsValid;
            public Transform Control;
            public Transform Bone;
            public Vector3 ControlPosition;
            public Quaternion ControlRotation;
            public Vector3 ControlScale;
            public Vector3 BonePosition;
            public Quaternion BoneRotation;
            public Vector3 BoneScale;
        }
    }
}
