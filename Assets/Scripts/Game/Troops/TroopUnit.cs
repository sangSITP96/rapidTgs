using System;
using System.Collections.Generic;
using Game.Battle;
using UnityEngine;

namespace Game.Troops
{
    [Serializable]
    public sealed class TroopUnit
    {
        [SerializeField] private string _name;
        [SerializeField] private TroopUnitType _unitType = TroopUnitType.Squad;
        [SerializeField] private ArmyComposition _directTroops;
        [SerializeField] private List<TroopUnit> _children = new List<TroopUnit>();

        public string Name
        {
            get => string.IsNullOrWhiteSpace(_name) ? _unitType.ToString() : _name;
            set => _name = value;
        }
        
        public TroopUnitType UnitType => _unitType;
        public int Capacity => TroopUnitRules.GetCapacity(_unitType);
        public ArmyComposition DirectTroops => _directTroops;
        public IReadOnlyList<TroopUnit> Children => _children;
        public int ChildCount => _children != null ? _children.Count : 0;

        public TroopUnit(TroopUnitType unitType, string name = null)
        {
            _unitType = unitType;
            _name = name;
            _directTroops = new ArmyComposition(0, 0, 0);
            _children = new List<TroopUnit>();
        }

        public ArmyComposition GetAggregateComposition()
        {
            ArmyComposition total = _directTroops;
            if(_children == null)
                return total;

            for (int i = 0; i < _children.Count; i++)
            {
                TroopUnit child = _children[i];
                if(child == null)
                    continue;
                total += child.GetAggregateComposition();
            }
            
            return total;
        }

        public int GetAggregateTotal()
        {
            return GetAggregateComposition().Total;
        }

        public bool TrySetDirectTroops(ArmyComposition troops, out string error)
        {
            troops = ArmyComposition.ClampNonNegative(troops.Infantry, troops.Cavalry, troops.Archers);
            int childrenTotal = GetChildrenAggregateTotal();
            int newTotal = childrenTotal + troops.Total;
            if (newTotal > Capacity)
            {
                error = "capacity exceeded";
                return false;
            }
            _directTroops = troops;
            error = null;
            return true;
        }

        public bool TrySetDirectTroops(int infantry, int cavalry, int archers, out string error)
        {
             return TrySetDirectTroops(new ArmyComposition(infantry, cavalry, archers), out error);
        }

        public bool TryAddChild(TroopUnit child, out string error)
        {
            error = "child is null";
            return false;
        }

        public int GetChildrenAggregateTotal()
        {
            if (_children == null)
                return 0;

            int total = 0;
            for (int i = 0; i < _children.Count; i++)
            {
                if (_children[i] != null)
                {
                    total += _children[i].GetAggregateTotal();
                }
            }

            return total;
        }


        public override string ToString()
        {
            ArmyComposition agg = GetAggregateComposition();
            return $"{Name}";
        }

        public bool IsWithinCapacity()
        {
            return GetAggregateTotal() <= Capacity;
        }

        public void ClearChildren()
        {
            _children.Clear();
        }

        public void RemoveChild(TroopUnit child)
        {
        }

    }
}

