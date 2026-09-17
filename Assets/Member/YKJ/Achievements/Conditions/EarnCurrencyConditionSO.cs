using Member.Wst.Scripts.Achievements.Conditions;
using Member.Wst.Scripts.Achievements.Datas;
using Member.Wst.Scripts.Currencys;
using UnityEngine;



[CreateAssetMenu(fileName = "EarnCurrencyCondition", menuName = "SO/achievement/Condition/Earn Currency")]
public sealed class EarnCurrencyConditionSO : AchievementConditionSO
{
    [SerializeField] private CurrencyType currencyType = CurrencyType.Gold;

    public override IAchievementCondition Create()
    {
        return new EarnCurrencyCondition(currencyType);
    }

    private class EarnCurrencyCondition : IAchievementCondition
    {
        private readonly CurrencyType _currencyType;
        private AchievementData _data;

        public EarnCurrencyCondition(CurrencyType currencyType)
        {
            _currencyType = currencyType;
        }

        public void Bind(AchievementData data)
        {
            _data = data;
            if (CurrencySystem.Instance == null)
                return;

            CurrencySystem.Instance.OnBalanceChanged += HandleBalanceChanged;
        }

        public void Unbind()
        {
            if (CurrencySystem.Instance != null)
                CurrencySystem.Instance.OnBalanceChanged -= HandleBalanceChanged;

            _data = null;
        }

        private void HandleBalanceChanged(CurrencyType type, int oldBalance, int newBalance)
        {
            if (_data == null || type != _currencyType)
                return;

            int gained = newBalance - oldBalance;
            if (gained <= 0)
                return;

            _data.AddDegree(gained);
        }
    }
}

