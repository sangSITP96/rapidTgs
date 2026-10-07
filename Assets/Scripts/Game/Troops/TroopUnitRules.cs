using UnityEngine;

namespace Game.Troops
{
    public static class TroopUnitRules
    {
        public const int MaxChildren = 4;

        public static int GetCapacity(TroopUnitType type)
        {
            switch (type)
            {
                case TroopUnitType.Squad: return 8;
                case TroopUnitType.Platoon: return 32;
                case TroopUnitType.Company: return 128;
                case TroopUnitType.Battalion: return 512;
                default: return 0;
            }
        }

        public static TroopUnitType? GetChildType(TroopUnitType type)
        {
            switch (type)
            {
                case TroopUnitType.Platoon: return TroopUnitType.Squad;
                case TroopUnitType.Company: return TroopUnitType.Platoon;
                case TroopUnitType.Battalion: return TroopUnitType.Company;
                default: return null;
            }
        }

        public static TroopUnitType? GetParentType(TroopUnitType type)
        {
            switch (type)
            {
                case TroopUnitType.Squad: return TroopUnitType.Platoon;
                case TroopUnitType.Platoon: return TroopUnitType.Company;
                case TroopUnitType.Company: return TroopUnitType.Battalion;
                default: return null;
            }
        }

        public static bool CanHaveChildren(TroopUnitType type)
        {
            return GetChildType(type).HasValue;
        }
    }
}
