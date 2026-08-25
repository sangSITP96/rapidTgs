using System;
using UnityEngine;

namespace Game.Currency
{
    [Serializable]
    public sealed class PlayerCurrencyState
    {
        [SerializeField, Min(0)] 
        private int gold;

        [SerializeField, Min(0)] 
        private int silver;

        public int Gold => gold;
        public int Silver => silver;

        public int GetBalance(CurrencyType type)
        {
            return type switch
            {
                CurrencyType.Gold => gold,
                CurrencyType.Silver => silver,
                _ => 0
            };
        }

        internal void SetBalance(CurrencyType type, int value)
        {
            value = Mathf.Max(0, value);

            switch (type)
            {
                case CurrencyType.Gold:
                    gold = value;
                    break;
                case CurrencyType.Silver:
                    silver = value;
                    break;
            }
        }

        internal void Add(CurrencyType type, int amount)
        {
            if (amount <= 0)
            {
                return;
            }
            
            SetBalance(type, GetBalance(type) + amount);
        }

        internal bool TrySpend(CurrencyType type, int amount)
        {
            if (amount <= 0)
                return false;
            
            int currentBalance = GetBalance(type);
            
            if(currentBalance < amount)
                return false;
            
            SetBalance(type, currentBalance - amount);
            
            return true;
        }
    }    
}

