using UnityEngine;
namespace YKJ_Script.Feedbacks
{
    [CreateAssetMenu(fileName = "FeedBack", menuName = "YKJ/FeedBackSO")]
    public class FeedbackSO : ScriptableObject
    {
        [HideInInspector] public int FeedBackId;
        public string Name;
        public void OnValidate()
        {
            if (string.IsNullOrEmpty(Name)) return;
            FeedBackId = Animator.StringToHash(Name);
        }
    }
}



