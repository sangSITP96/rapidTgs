using System;
using Game.Troops;
using UnityEngine;

namespace Game.Recruitment
{
    /// <summary>
    /// Runtime view of one mercenary offer: definition + live availability.
    /// </summary>
    [Serializable]
    public sealed class MercenaryOfferRuntimeState
    {
        [SerializeField] private string offerId;
        [SerializeField] private string displayName;
        [SerializeField] private TroopUnitType unitType;
        [SerializeField] private int infantry;
        [SerializeField] private int cavalry;
        [SerializeField] private int archers;
        [SerializeField] private int goldPrice;
        [SerializeField] private int available;

        public string OfferId => offerId;
        public string DisplayName => displayName;
        public TroopUnitType UnitType => unitType;
        public int Infantry => infantry;
        public int Cavalry => cavalry;
        public int Archers => archers;
        public int GoldPrice => goldPrice;
        public int Available => available;
        public bool IsSoldOut => available <= 0;

        public MercenaryOfferRuntimeState(MercenaryOfferDefinition definition, int available)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            offerId = definition.OfferId;
            displayName = definition.DisplayName;
            unitType = definition.UnitType;
            infantry = definition.Infantry;
            cavalry = definition.Cavalry;
            archers = definition.Archers;
            goldPrice = definition.GoldPrice;
            this.available = Mathf.Max(0, available);
        }

        internal void SetAvailable(int value)
        {
            available = Mathf.Max(0, value);
        }

        internal bool TryDecrease(int quantity)
        {
            if (quantity <= 0 || quantity > available)
                return false;

            available -= quantity;
            return true;
        }
    }
}
