using System;
using Member.Wst.Scripts.Achievements.Datas;
using UnityEngine;

namespace Member.Wst.Scripts.Achievements.Conditions
{
    [CreateAssetMenu(fileName = "CounterCondition", menuName = "SO/achievement/Condition/Recorded Counter")]
    public sealed class RecordedCounterConditionSO : AchievementConditionSO
    {
        [SerializeField] private AchievementCounter counter;
        public override IAchievementCondition Create() => new Condition(counter);

        private sealed class Condition : IAchievementCondition
        {
            private readonly AchievementCounter _counter;
            private AchievementData _data;
            public Condition(AchievementCounter counter) => _counter = counter;
            public void Bind(AchievementData data)
            {
                Unbind();
                _data = data;
                AchievementProgressRecord.OnChanged += Refresh;
                Refresh(_counter);
            }
            public void Unbind()
            {
                AchievementProgressRecord.OnChanged -= Refresh;
                _data = null;
            }
            private void Refresh(AchievementCounter counter)
            {
                if (_data == null || counter != _counter) return;
                _data.AddDegree(Math.Max(0, AchievementProgressRecord.GetCount(_counter)
                    - _data.AchieveSaveData.nowAchievementDegree));
            }
        }
    }
}
