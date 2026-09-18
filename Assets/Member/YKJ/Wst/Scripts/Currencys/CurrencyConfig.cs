using System.Linq;
using UnityEngine;

namespace Member.Wst.Scripts.Currencys
{
    [CreateAssetMenu(fileName = "CurrencyConfig", menuName = "SO/CurrencyConfig", order = 0)]
    public sealed class CurrencyConfig : ScriptableObject
    {
        [System.Serializable]
        public class CurrencyEntry
        {
            public CurrencyType type;
            public string displayName;
            public Sprite icon;
            public int startingBalance;
            public int maxBalance;
        }

        public CurrencyEntry[] currencies;

        public CurrencyEntry GetEntry(CurrencyType type)
        {
            CurrencyEntry test = currencies.FirstOrDefault(x => x.type == type);

            return test;
        }
    }
}