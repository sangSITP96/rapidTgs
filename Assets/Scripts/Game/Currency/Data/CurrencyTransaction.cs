using System;

namespace Game.Currency
{
    [Serializable]
    public sealed class CurrencyTransaction
    {
        public CurrencyType Currency;
        public CurrencyTransactionType TransactionType;
        public int Amount;
        public int BalanceAfter;
        public string Reason;
        public DateTime Timestamp;

        public CurrencyTransaction(CurrencyType currency,
            CurrencyTransactionType transactionType,
            int amount,
            int balanceAfter,
            string reason)
        {
            Currency = currency;
            TransactionType = transactionType;
            Amount = amount;
            BalanceAfter = balanceAfter;
            Reason = reason;
            Timestamp = DateTime.UtcNow;
        }
    }
}