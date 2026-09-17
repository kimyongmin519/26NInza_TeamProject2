using KimLIb.EventSystem;
using Member.Wst.Scripts.Achievements.Datas;
using UnityEngine;
using static QuestEvent;

namespace Member.Wst.Scripts.Achievements.Conditions
{
    [CreateAssetMenu(fileName = "StartGameCondition", menuName = "SO/achievement/Condition/Start Game")]
    public sealed class StartGameEarnConditionSO : AchievementConditionSO
    {
        [SerializeField] private EventChannelSO EventChannel;

        public override IAchievementCondition Create()
        {
            return new StartGameEarnCondition(EventChannel);
        }

        private sealed class StartGameEarnCondition : IAchievementCondition
        {
            private readonly EventChannelSO _eventChannel;
            private AchievementData _data;

            public StartGameEarnCondition(EventChannelSO eventChannel)
            {
                _eventChannel = eventChannel;
            }

            public void Bind(AchievementData data)
            {
                _data = data;
                _eventChannel?.AddListener<GameStartedEvent>(HandleGameStarted);
            }

            public void Unbind()
            {
                _eventChannel?.RemoveListener<GameStartedEvent>(HandleGameStarted);
                _data = null;
            }

            private void HandleGameStarted(GameStartedEvent evt)
            {
                _data?.AddDegree(1);
            }
        }
    }
}
