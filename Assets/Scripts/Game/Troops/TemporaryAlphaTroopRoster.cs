using System.Collections.Generic;
using Game.Battle;
using UnityEngine;

namespace Game.Troops
{
    /// <summary>
    /// TEMPORARY Alpha/test seed data only — not a production roster/inventory/recruitment system.
    /// Swap this component later for a real roster that implements <see cref="ITroopUnitRoster"/>.
    /// </summary>
    public sealed class TemporaryAlphaTroopRoster : MonoBehaviour, ITroopUnitRoster
    {
        [SerializeField] private string _commanderId = "local-commander";

        private readonly List<TroopUnit> _allUnits = new List<TroopUnit>();
        private readonly HashSet<TroopUnit> _deployed = new HashSet<TroopUnit>();
        private bool _seeded;

        public string CommanderId => _commanderId;

        private void Awake()
        {
            EnsureSeeded();
        }

        public void SetCommanderId(string commanderId)
        {
            if (!string.IsNullOrWhiteSpace(commanderId))
                _commanderId = commanderId;
        }

        public IReadOnlyList<TroopUnit> GetAvailableUnits()
        {
            EnsureSeeded();
            var available = new List<TroopUnit>(_allUnits.Count);
            for (int i = 0; i < _allUnits.Count; i++)
            {
                TroopUnit unit = _allUnits[i];
                if (unit != null && !_deployed.Contains(unit))
                    available.Add(unit);
            }

            return available;
        }

        public bool TryMarkDeployed(TroopUnit unit)
        {
            if (unit == null)
                return false;
            return _deployed.Add(unit);
        }

        public void ResetDeployedForTesting()
        {
            _deployed.Clear();
        }

        /// <summary>
        /// Extra seed units for reinforcement testing (Support Commander).
        /// </summary>
        public TroopUnit CreateReinforcementUnit(string name, TroopUnitType type, int infantry, int cavalry, int archers)
        {
            var unit = new TroopUnit(type, name);
            unit.TrySetDirectTroops(infantry, cavalry, archers, out _);
            _allUnits.Add(unit);
            return unit;
        }

        private void EnsureSeeded()
        {
            if (_seeded)
                return;

            _seeded = true;
            _allUnits.Clear();

            // Seed matches client SELECT TROOPS example (flat DirectTroops — hierarchy TryAddChild still stubbed).
            _allUnits.Add(Build(TroopUnitType.Platoon, "Platoon A", 16, 8, 8));
            _allUnits.Add(Build(TroopUnitType.Platoon, "Platoon B", 20, 4, 8));
            _allUnits.Add(Build(TroopUnitType.Squad, "Squad A", 8, 0, 0));
        }

        private static TroopUnit Build(TroopUnitType type, string name, int infantry, int cavalry, int archers)
        {
            var unit = new TroopUnit(type, name);
            if (!unit.TrySetDirectTroops(new ArmyComposition(infantry, cavalry, archers), out string error))
                Debug.LogWarning($"[TemporaryAlphaTroopRoster] Failed to seed {name}: {error}");
            return unit;
        }
    }
}
