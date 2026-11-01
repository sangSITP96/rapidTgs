using System;
using Game.Battle;
using Game.Troops;
using UnityEngine;

namespace Game.Recruitment
{
    /// <summary>
    /// Configurable mercenary offer template (identity, composition, price, stock).
    /// Runtime availability is tracked separately by <see cref="MercenaryPool"/>.
    /// </summary>
    [Serializable]
    public sealed class MercenaryOfferDefinition
    {
        [SerializeField] private string offerId = "merc-squad";
        [SerializeField] private string displayName = "Mercenary Squad";
        [SerializeField] private TroopUnitType unitType = TroopUnitType.Squad;
        [SerializeField, Min(0)] private int infantry = 8;
        [SerializeField, Min(0)] private int cavalry;
        [SerializeField, Min(0)] private int archers;
        [SerializeField, Min(0)] private int goldPrice = 50;
        [SerializeField, Min(0)] private int initialAvailable = 10;

        public string OfferId => string.IsNullOrWhiteSpace(offerId) ? displayName : offerId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? OfferId : displayName;
        public TroopUnitType UnitType => unitType;
        public int Infantry => infantry;
        public int Cavalry => cavalry;
        public int Archers => archers;
        public int GoldPrice => goldPrice;
        public int InitialAvailable => initialAvailable;

        public ArmyComposition Composition => new ArmyComposition(infantry, cavalry, archers);

        public MercenaryOfferDefinition()
        {
        }

        public MercenaryOfferDefinition(
            string offerId,
            string displayName,
            TroopUnitType unitType,
            int infantry,
            int cavalry,
            int archers,
            int goldPrice,
            int initialAvailable)
        {
            this.offerId = offerId;
            this.displayName = displayName;
            this.unitType = unitType;
            this.infantry = Mathf.Max(0, infantry);
            this.cavalry = Mathf.Max(0, cavalry);
            this.archers = Mathf.Max(0, archers);
            this.goldPrice = Mathf.Max(0, goldPrice);
            this.initialAvailable = Mathf.Max(0, initialAvailable);
        }
    }
}
