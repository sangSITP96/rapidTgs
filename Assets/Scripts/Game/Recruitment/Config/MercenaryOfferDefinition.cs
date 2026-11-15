using System;
using System.Text;
using Game.Battle;
using Game.Seasons;
using Game.Troops;
using UnityEngine;

namespace Game.Recruitment
{
    /// <summary>
    /// Configurable mercenary offer template (identity, composition, price, stock, UI labels).
    /// Runtime availability is tracked separately by <see cref="MercenaryPool"/>.
    /// </summary>
    [Serializable]
    public sealed class MercenaryOfferDefinition
    {
        [SerializeField] private string offerId = "merc-squad";
        [Tooltip("Optional override. If empty, UNIT column shows TroopUnitType name (Squad/Platoon/…).")]
        [SerializeField] private string displayName;
        [SerializeField] private TroopUnitType unitType = TroopUnitType.Squad;
        [SerializeField, Min(0)] private int infantry = 8;
        [SerializeField, Min(0)] private int cavalry;
        [SerializeField, Min(0)] private int archers;
        [SerializeField, Min(0)] private int goldPrice = 50;
        [SerializeField, Min(0)] private int initialAvailable = 10;
        [Header("Terminal UI labels (display-only placeholders)")]
        [SerializeField] private SeasonId seasonAffinity = SeasonId.Autumn;
        [SerializeField] private MercenaryCombatAffinity combatAffinity = MercenaryCombatAffinity.Attack;

        public string OfferId => string.IsNullOrWhiteSpace(offerId) ? UnitColumnLabel : offerId;

        /// <summary>UNIT column — prefer short formation name matching client mock.</summary>
        public string UnitColumnLabel =>
            string.IsNullOrWhiteSpace(displayName) ? unitType.ToString() : displayName;

        public string DisplayName => UnitColumnLabel;
        public TroopUnitType UnitType => unitType;
        public int Infantry => infantry;
        public int Cavalry => cavalry;
        public int Archers => archers;
        public int GoldPrice => goldPrice;
        public int InitialAvailable => initialAvailable;
        public SeasonId SeasonAffinity => seasonAffinity;
        public MercenaryCombatAffinity CombatAffinity => combatAffinity;
        public int TroopCount => infantry + cavalry + archers;

        public ArmyComposition Composition => new ArmyComposition(infantry, cavalry, archers);

        public string TypeLabel => FormatTypeLabel(infantry, cavalry, archers);

        public MercenaryOfferDefinition()
        {
        }

        public MercenaryOfferDefinition(
            string offerId,
            TroopUnitType unitType,
            int infantry,
            int cavalry,
            int archers,
            int goldPrice,
            int initialAvailable,
            SeasonId seasonAffinity,
            MercenaryCombatAffinity combatAffinity,
            string displayName = null)
        {
            this.offerId = offerId;
            this.displayName = displayName;
            this.unitType = unitType;
            this.infantry = Mathf.Max(0, infantry);
            this.cavalry = Mathf.Max(0, cavalry);
            this.archers = Mathf.Max(0, archers);
            this.goldPrice = Mathf.Max(0, goldPrice);
            this.initialAvailable = Mathf.Max(0, initialAvailable);
            this.seasonAffinity = seasonAffinity;
            this.combatAffinity = combatAffinity;
        }

        public static string FormatTypeLabel(int infantry, int cavalry, int archers)
        {
            var sb = new StringBuilder();
            if (infantry > 0)
                AppendType(sb, "Infantry");
            if (cavalry > 0)
                AppendType(sb, "Cavalry");
            if (archers > 0)
                AppendType(sb, "Archers");
            return sb.Length > 0 ? sb.ToString() : "—";
        }

        private static void AppendType(StringBuilder sb, string label)
        {
            if (sb.Length > 0)
                sb.Append('/');
            sb.Append(label);
        }
    }
}
