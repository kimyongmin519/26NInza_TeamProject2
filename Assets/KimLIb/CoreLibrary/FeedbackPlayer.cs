using UnityEngine;

namespace GGMLib.CoreLibrary
{
    public class FeedbackPlayer : MonoBehaviour
    {
        private AbstractFeedback[] _feedbacks;
        
        private void Awake()
        {
            _feedbacks = GetComponentsInChildren<AbstractFeedback>();
        }

        public void PlayAllFeedbacks()
        {
            foreach (AbstractFeedback feedback in _feedbacks)
            {
                feedback.PlayFeedback();
            }
        }

        public void StopAllFeedbacks()
        {
            foreach (AbstractFeedback feedback in _feedbacks)
            {
                feedback.StopFeedback();
            }
        }
    }
}