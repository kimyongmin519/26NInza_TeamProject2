using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MoreMountains.Feedbacks;
using KimLIb.EventSystem;


namespace YKJ_Script.Feedbacks
{
    public class FeedbackContainer : MonoBehaviour
    {
        [SerializeField] private List<Feedbacks> FeedBackList;
        [SerializeField] private EventChannelSO FeedBackChannel;
        private Dictionary<int, MMF_Player> _FeedbackDic;
        private void Awake()
        {
            _FeedbackDic = FeedBackList.ToDictionary(value => value.FeedBackData.FeedBackId, key => key.Player);
            FeedBackChannel.AddListener<PlayFeedBack>(HandlePlayFeedBack);
         
        }
        private void OnDestroy()
        {
            FeedBackChannel.RemoveListener<PlayFeedBack>(HandlePlayFeedBack);
        }
        private void HandlePlayFeedBack(PlayFeedBack evt)
        {
            PlayFeedBack(evt.FeedbackId);
        }
        public void PlayFeedBack(int feedback)
        {
            if (_FeedbackDic.ContainsKey(feedback))
                _FeedbackDic[feedback].PlayFeedbacks();
            else
                Debug.LogError($"키에 해당하는 피드백이 존재하지 않습니다{feedback}");
        }
    }

    [System.Serializable]
    public class Feedbacks
    {
        public FeedbackSO FeedBackData;
        public MMF_Player Player;

    }
    public class PlayFeedBack : GameEvent
    {
        public int FeedbackId;
        public PlayFeedBack Init(int feedBackName)
        {
            FeedbackId = feedBackName;
            return this;
        }
    }
}




