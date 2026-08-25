using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Currency
{
    public sealed class CurrencySystem : MonoBehaviour
    {
        [Header("Configuration")] 
        [SerializeField]
        private CurrencySettings settings;

        [Header("Runtime Stste")] [SerializeField]
        private PlayerCurrencyState state = new PlayerCurrencyState();
        
        private readonly List<CurrencyTransaction> _transactions = new ();
        
        public PlayerCurrencyState State => state;

        public int Gold => state.Gold;
        public int Silver => state.Silver;
        
        public IReadOnlyList<CurrencyTransaction> Transactions => _transactions;

        public event Action<CurrencyType, int> BalanceChanged;
        public event Action<CurrencyTransaction> TransactionRecorded;

        public bool HasEnoughGold(int amount)
        {
            return amount >= 0 && Gold >= amount;
        }

        public bool HasEnoughSilver(int amount)
        {
            return amount >= 0 && Silver >= amount;
        }

        public bool HasEnough(CurrencyType currency, int amount)
        {
            if(amount < 0)
                return false;
            
            return state.GetBalance(currency)  >= amount;
        }

        public void AddGold(int amount, string reason)
        {
            AddCurrency(CurrencyType.Gold, amount, reason);
        }

        public bool TrySpendGold(int amount, string reason)
        {
            return TrySpendCurrency(CurrencyType.Gold, amount, reason);
        }

        public void AwardSilver(int amount, string reason)
        {
            AddCurrency(CurrencyType.Silver, amount, reason);
        }

        public void AddSilver(int amount, string reason)
        {
            AddCurrency(CurrencyType.Silver, amount, reason);
        }

        public bool TrySpendSilver(int amount, string reason)
        {
            return TrySpendCurrency(CurrencyType.Silver, amount, reason);
        }

        public void AwardDefaultOperationSilver(
            string reason = "Operation Completed")
        {
            if(settings == null)
                return;
            
            AwardSilver(settings.DefaultOperationSilverReward,
                reason);
        }

        public void ApplySeasonReset()
        {
            if(settings == null)
                return;
            
            ApplyReset(CurrencyType.Gold,
                settings.GoldResetMode,
                settings.GoldResetValue);
            
            ApplyReset(CurrencyType.Silver,
                settings.SilverResetMode,
                settings.SilverResetValue);
        }

        public void ResetToStartingBalances()
        {
            if(settings == null)
                return;
            
            SetBalanceAndRecordReset(
                CurrencyType.Gold,
                settings.StartingGold,
                "Reset To Starting Balance");
            
            SetBalanceAndRecordReset(
                CurrencyType.Silver,
                settings.StartingSilver,
                "Reset To Starting Balance");
        }

        private void NotifyBalanceChanged(CurrencyType currency)
        {
            BalanceChanged?.Invoke(currency, state.GetBalance(currency));
        }

        private CurrencyTransaction RecordTransaction(
            CurrencyType currency,
            CurrencyTransactionType transactionType,
            int amount,
            string reason)
        {
            CurrencyTransaction transaction = new CurrencyTransaction(
                currency,
                transactionType,
                amount,
                state.GetBalance(currency),
                string.IsNullOrWhiteSpace(reason)
                ? "Unspecified"
                : reason);
            
            _transactions.Add(transaction);
            
            return transaction;
        }

        private void SetBalanceAndRecordReset(
            CurrencyType currency,
            int newValue,
            string reason)
        {
            var oldValue = state.GetBalance(currency);
            
            newValue = Mathf.Max(0, newValue);
            
            if(oldValue == newValue)
                return;
            
            state.SetBalance(currency, newValue);
            
            var difference = newValue - oldValue;
            
            CurrencyTransaction transaction = RecordTransaction(
                currency,
                CurrencyTransactionType.Reset,
                difference,
                reason);
            
            NotifyBalanceChanged(currency);
            TransactionRecorded?.Invoke(transaction);
        }

        private void ApplyReset(
            CurrencyType currency,
            CurrrencyResetMode resetMode,
            int resetValue)
        {
            switch (resetMode)
            {
                case CurrrencyResetMode.Keep:
                    return;
                
                case CurrrencyResetMode.ResetToValue:
                    SetBalanceAndRecordReset(currency, resetValue, "Season Reset");
                    break;
            }
        }

        private bool TrySpendCurrency(
            CurrencyType currency,
            int amount,
            string reason)
        {
            if (amount <= 0)
                return false;

            if (!state.TrySpend(currency, amount))
                return false;
            
            CurrencyTransaction transaction =
                RecordTransaction(currency, 
                    CurrencyTransactionType.Spent,
                    -amount, 
                    reason);
            
            NotifyBalanceChanged(currency);
            TransactionRecorded?.Invoke(transaction);
            
            return true;
        }

        private void AddCurrency(CurrencyType currency,
            int amount,
            string reason)
        {
            if (amount <= 0)
                return;
            
            state.Add(currency, amount);

            CurrencyTransaction transaction =
                RecordTransaction(currency,
                    CurrencyTransactionType.Added,
                    amount,
                    reason);
            
            NotifyBalanceChanged(currency);
            TransactionRecorded?.Invoke(transaction);
        }
    }
}