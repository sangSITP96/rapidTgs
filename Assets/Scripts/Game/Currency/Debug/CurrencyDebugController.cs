using UnityEngine;

namespace Game.Currency
{
    public class CurrencyDebugController : MonoBehaviour
    {
        [SerializeField] private CurrencySystem currencySystem;

        [Header("Debug Amount")] 
        [SerializeField, Min(1)]
        private int goldAmount = 10;
        
        [SerializeField, Min(1)]
        private int silverAmount = 10;

        [ContextMenu("Add Gold")]
        private void AddGold()
        {
            currencySystem?.AddGold(goldAmount, "Debug Add Gold");
        }

        [ContextMenu("Spend Gold")]
        private void SpendGold()
        {
            currencySystem?.TrySpendGold(goldAmount, "Debug Spend Gold");
        }

        [ContextMenu("Award Silver")]
        private void AwardSilver()
        {
            currencySystem?.AwardSilver(silverAmount, "Debug Award Silver");
        }
        
        [ContextMenu("Spend Silver")]
        private void SpendSilver()
        {
            currencySystem?.TrySpendSilver(silverAmount, "Debug Spend Silver");
        }
        
        [ContextMenu("Apply Season Reset")]
        private void ApplySeasonReset()
        {
            currencySystem?.ApplySeasonReset();
        }
        
        [ContextMenu("Reset to Starting Balances")]
        private void ResetToStartingBalances()
        {
            currencySystem?.ResetToStartingBalances();
        }

        [ContextMenu("PrintEventLog()")]
        private void PrintEventLog()
        {
            if(currencySystem == null)
                return;

            foreach (var transaction in currencySystem.Transactions)
            {
                Debug.Log($"[{transaction.Timestamp:u}]"+
                          $"[{transaction.Currency} |"+
                          $"[{transaction.TransactionType} |"+
                          $"[{transaction.Amount:+#;-#;0} |"+
                          $" Balance: {transaction.BalanceAfter} |"+
                          $"{transaction.Reason}");
            }
        }
    }
}

