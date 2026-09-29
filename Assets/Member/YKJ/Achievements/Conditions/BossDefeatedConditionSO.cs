using System;
using Member.Wst.Scripts.Achievements.Datas;
using UnityEngine;

namespace Member.Wst.Scripts.Achievements.Conditions
{
    [CreateAssetMenu(fileName = "BossDefeatedCondition", menuName = "SO/achievement/Condition/Boss Defeated")]
    public sealed class BossDefeatedConditionSO : AchievementConditionSO
    {
        [SerializeField, Tooltip("BossDefeatReporter의 Boss Name과 동일한 이름. 앞뒤 공백은 무시합니다.")]
        private string bossName;

        public override IAchievementCondition Create() => new Condition(bossName);

        private sealed class Condition : IAchievementCondition
        {
            private readonly string _bossName;
            private AchievementData _data;

            public Condition(string name) => _bossName = name?.Trim();

            public void Bind(AchievementData data)
            {
                Unbind();
                _data = data;
                BossDefeatRecord.OnRecorded += HandleRecorded;
                Refresh();
            }

            public void Unbind()
            {
                BossDefeatRecord.OnRecorded -= HandleRecorded;
                _data = null;
            }

            private void HandleRecorded(string name)
            {
                if (string.Equals(name, _bossName, StringComparison.Ordinal))
                    Refresh();
            }

            private void Refresh()
            {
                if (_data == null || string.IsNullOrWhiteSpace(_bossName))
                    return;
                int count = BossDefeatRecord.GetCount(_bossName);
                _data.AddDegree(Math.Max(0, count - _data.AchieveSaveData.nowAchievementDegree));
            }
        }
    }
}
