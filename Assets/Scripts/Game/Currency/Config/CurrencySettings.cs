using UnityEngine;

namespace Game.Currency
{
    [CreateAssetMenu(fileName = "CurrentSettings", menuName = "Game/Currency/Currency Settings")]
    public sealed class CurrencySettings : ScriptableObject
    {
        [Header("Starting Balances")] [SerializeField, Min(0)]
        private int startingGold;
        
        [SerializeField]
        private int startingSilver;

        [Header("Silver Rewards")] [SerializeField, Min(0)]
        private int defaultOperationSilverReward = 10;
        
        [Header("Season Reset - Gold")]
        [SerializeField]
        private CurrrencyResetMode goldResetMode = CurrrencyResetMode.Keep;

        [SerializeField, Min(0)] private int goldResetValue;
        
        [Header("Season Reset - Silver")]
        [SerializeField]
        private CurrrencyResetMode silverResetMode = CurrrencyResetMode.Keep;
        
        [SerializeField, Min(0)]
        private int silverResetValue;
        
        public int StartingGold => startingGold;
        public int StartingSilver => startingSilver;
        
        public int DefaultOperationSilverReward => defaultOperationSilverReward;
        
        public CurrrencyResetMode GoldResetMode => goldResetMode; 
        
        public int GoldResetValue => goldResetValue;
        
        public CurrrencyResetMode SilverResetMode => silverResetMode;
        
        public int SilverResetValue => silverResetValue;
    }
}