using System.Linq;
using Member.Wst.Scripts.Achievements.Datas;
using UnityEngine;

namespace Member.Wst.Scripts.Achievements.Conditions
{
    [CreateAssetMenu(fileName = "AllBossesDefeatedCondition", menuName = "SO/achievement/Condition/All Bosses Defeated")]
    public sealed class AllBossesDefeatedConditionSO : AchievementConditionSO
    {
        [SerializeField] private string[] bossNames;
        public override IAchievementCondition Create() => new Condition(bossNames);

        private sealed class Condition : IAchievementCondition
        {
            private readonly string[] _names;
            private AchievementData _data;
            public Condition(string[] names) => _names = names?.ToArray() ?? new string[0];
            public void Bind(AchievementData data)
            {
                Unbind();
                _data = data;
                BossDefeatRecord.OnRecorded += Refresh;
                Refresh(null);
            }
            public void Unbind()
            {
                BossDefeatRecord.OnRecorded -= Refresh;
                _data = null;
            }
            private void Refresh(string name)
            {
                if (_data != null && _names.Length > 0 &&
                    _names.All(n => !string.IsNullOrWhiteSpace(n) && BossDefeatRecord.GetCount(n) > 0))
                    _data.AddDegree(_data.AchievementDataSO.TargetDegree);
            }
        }
    }
}
