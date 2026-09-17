using System;
using System.Collections.Generic;
using UnityEngine;

namespace Member.Wst.Scripts.Currencys
{
    public class CurrencySystem : MonoSingleton<CurrencySystem>
    {
        [Header("CurrencyConfig")]
        [SerializeField] private CurrencyConfig currencyConfig;

        private readonly Dictionary<CurrencyType, int> balances = new();
        
        public event Action<CurrencyType, int, int> OnBalanceChanged;

        protected override void Awake()
        {
            base.Awake();

            InitializeBalances();
        }
        
        //현재 런타임에서 가지고 있는 Currency값을 가져오는 함수
        public int GetBalance(CurrencyType type)
        {
            return balances.TryGetValue(type, out int balance) ? balance : 0;
        }
        
        //사용하려는 값이 더 많은지 적은지 확인하는 함수
        public bool HasEnough(CurrencyType type, int amount)
        {
            return GetBalance(type) >= amount;
        }
        
        //돈 추가하는 함수
        public void Earn(CurrencyType type, int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CurrencySystem] 넣으려는 돈은 마이너스가 되면 안됨");
                return;
            }
            if (!balances.ContainsKey(type))
            {
                Debug.LogWarning($"[CurrencySystem] 가져오려는 타입이 없음 {type}.");
                return;
            }
            
            CurrencyConfig.CurrencyEntry entry = currencyConfig != null ? currencyConfig.GetEntry(type) : null;
            int oldBalance = balances[type];
            int newBalance = oldBalance + amount;
            
            if (entry != null && newBalance > entry.maxBalance)
                newBalance = entry.maxBalance;
            
            balances[type] = newBalance;
            OnBalanceChanged?.Invoke(type, oldBalance, newBalance);
        }
        
        //돈 빼내는 함수
        public bool Spend(CurrencyType type, int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CurrencySystem] 뺄려는 돈은 마이너스가 되면 안됩니다. 매개변수 amount확인 바람");
                return false;
            }
            
            if (!balances.ContainsKey(type))
            {
                Debug.LogWarning($"[CurrencySystem] 이 타입은 balances에 없는 타입입니다 {type}.");
                return false;
            }
            
            int oldBalance = balances[type];
            if (oldBalance < amount)
            {
                Debug.Log("돈이 부족합니다");
                return false;   
            }
            
            int newBalance = oldBalance - amount;
            balances[type] = newBalance;
            OnBalanceChanged?.Invoke(type, oldBalance, newBalance);
            return true;
        }
        
        public Sprite GetIcon(CurrencyType type)
        {
            CurrencyConfig.CurrencyEntry entry = currencyConfig != null ? currencyConfig.GetEntry(type) : null;
            return entry != null ? entry.icon : null;
        }
        
        public string GetDisplayName(CurrencyType type)
        {
            CurrencyConfig.CurrencyEntry entry = currencyConfig != null ? currencyConfig.GetEntry(type) : null;
            return entry != null ? entry.displayName : string.Empty;
        }

        //처음 값을 저장 있으면 불러오고 아니면 컨피그에 있는 초기값 쓰는 매서드 (아직은 저장없음)
        private void InitializeBalances()
        {
            balances.Clear();
            if (currencyConfig == null || currencyConfig.currencies == null)
            {
                Debug.LogError("[CurrencySystem] CurrencyConfig쪽이 비어있음", this);
                return;
            }
            
            for (int i = 0; i < currencyConfig.currencies.Length; i++)
            {
                CurrencyConfig.CurrencyEntry entry = currencyConfig.currencies[i];
                if (entry == null)
                    continue;
                
                if (balances.ContainsKey(entry.type))
                {
                    Debug.LogWarning($"[CurrencySystem] {entry.type} 이 타입이 컨피그 안에 없음");
                    continue;
                }
                
                balances[entry.type] = Mathf.Max(0, entry.startingBalance);
            }
        }
    }
}
