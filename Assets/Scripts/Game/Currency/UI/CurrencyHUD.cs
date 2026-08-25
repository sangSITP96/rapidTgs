using TMPro;
using UnityEngine;

namespace  Game.Currency.UI
{
    public class CurrencyHUD : MonoBehaviour
    {
        [SerializeField]
        private CurrencySystem currencySystem;
        
        [Header("Gold")]
        [SerializeField]
        private TMP_Text goldText;
        
        [Header("Silver")]
        [SerializeField]
        private TMP_Text silverText;

        private void OnEnable()
        {
            if (currencySystem != null)
                currencySystem.BalanceChanged += OnBalanceChanged;

            Refresh();
        }

        private void OnDisable()
        {
            if(currencySystem != null)
                currencySystem.BalanceChanged -= OnBalanceChanged;
        }

        public void Refresh()
        {
            if (currencySystem == null)
                return;
            
            if(goldText != null)
                goldText.SetText("{0}", currencySystem.Gold);
            
            if(silverText != null)
                silverText.SetText("{0}", currencySystem.Silver);
        }

        private void OnBalanceChanged(CurrencyType currency, int newBalance)
        {
            switch(currency)
            {
                case CurrencyType.Gold:
                    if(goldText != null)
                        goldText.SetText("{0}", newBalance);
                    
                    break;
                
                case CurrencyType.Silver:
                    if(silverText != null)
                        silverText.SetText("{0}", newBalance);
                    
                    break;
            }
        }
    } 
}

