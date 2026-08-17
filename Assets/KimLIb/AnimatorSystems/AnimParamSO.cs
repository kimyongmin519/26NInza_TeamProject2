using UnityEngine;

namespace KimLIb.AnimatorSystems
{
    [CreateAssetMenu(fileName = "Anim param", menuName = "KimLib/AnimatorSystems/Anim param", order = 0)]
    public class AnimParamSO : ScriptableObject
    {
        [field: SerializeField] public string ParamName { get; private set; }
        [field: SerializeField] public int ParamHash { get; private set; }

        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(ParamName))
            {
                ParamHash = Animator.StringToHash(ParamName);
            }
        }
    }
}