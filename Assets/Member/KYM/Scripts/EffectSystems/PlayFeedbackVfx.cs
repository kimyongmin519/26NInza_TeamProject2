using KimLIb.DataSystems;
using Member.KYM.Scripts.EffectSystems.FeedbackVfx;
using UnityEngine;

namespace Member.KYM.Scripts.EffectSystems
{
    public class PlayFeedbackVfx : MonoBehaviour, IPlayableVfx
    {
        [field: SerializeField] public AssetNameSO VfxName { get; private set; }
        [field: SerializeField] public float Duration { get; private set; }
        
        private IFeedbackVfx[] _feedbackVfxs;
        
        [Header("피드백 컴포넌트들을 사용할 것 인가?")]
        [SerializeField] private bool isFeedbackVfx;
        private void Awake()
        {
            
            if (isFeedbackVfx)
                _feedbackVfxs = GetComponents<IFeedbackVfx>();
        }

        public void PlayVfx(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            PlayVfx();
        }

        public void PlayVfx()
        {

            if (_feedbackVfxs != null)
            {
                foreach (IFeedbackVfx feedbackVfx in _feedbackVfxs)
                {
                    feedbackVfx.PlayFeedbackVfx();
                }
            }
        }

        public void StopVfx()
        {
            if (_feedbackVfxs != null)
            {
                foreach (IFeedbackVfx feedbackVfx in _feedbackVfxs)
                {
                    feedbackVfx.StopFeedbackVfx();
                }
            }
            
        }
    }
}