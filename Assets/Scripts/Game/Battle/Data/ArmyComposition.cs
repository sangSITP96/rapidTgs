using System;
using UnityEngine;

namespace Game.Battle
{
    [Serializable]
    public struct ArmyComposition
    {
        [SerializeField, Min(0)] private int infantry;
        [SerializeField, Min(0)] private int cavalry;
        [SerializeField, Min(0)] private int archers;

        public int Infantry => infantry;
        public int Cavalry => cavalry;
        public int Archers => archers;

        public int Total => infantry + cavalry + archers;

        public bool IsEmpty => Total <= 0;

        public ArmyComposition(int infantry, int cavalry, int archers)
        {
            this.infantry = Mathf.Max(0, infantry);
            this.cavalry = Mathf.Max(0, cavalry);
            this.archers = Mathf.Max(0, archers);
        }

        public int Get(TroopType type)
        {
            switch (type)
            {
                case TroopType.Infantry: return infantry;
                case TroopType.Cavalry: return cavalry;
                case TroopType.Archers: return archers;
                default: return 0;
            }
        }

        public float Fraction(TroopType type)
        {
            int total = Total;
            if (total <= 0)
                return 0f;
            return Get(type) / (float)total;
        }

        public ArmyComposition With(TroopType type, int count)
        {
            count = Mathf.Max(0, count);
            switch (type)
            {
                case TroopType.Infantry:
                    return new ArmyComposition(count, cavalry, archers);
                case TroopType.Cavalry:
                    return new ArmyComposition(infantry, count, archers);
                case TroopType.Archers:
                    return new ArmyComposition(infantry, cavalry, count);
                default:
                    return this;
            }
        }

        public static ArmyComposition ClampNonNegative(int infantry, int cavalry, int archers)
        {
            return new ArmyComposition(infantry, cavalry, archers);
        }

        public static ArmyComposition operator -(ArmyComposition left, ArmyComposition right)
        {
            return new ArmyComposition(
                left.infantry - right.infantry,
                left.cavalry - right.cavalry,
                left.archers - right.archers);
        }

        public override string ToString()
        {
            return $"Inf={infantry}, Cav={cavalry}, Arch={archers} (Total={Total})";
        }
    }
}
