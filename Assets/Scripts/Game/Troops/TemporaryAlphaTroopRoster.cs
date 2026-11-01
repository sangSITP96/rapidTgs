using System.Collections.Generic;
using Game.Battle;
using Game.Persistence;
using UnityEngine;

namespace Game.Troops
{
    /// <summary>
    /// TEMPORARY Alpha/test roster — also the Phase 19 destination for hired mercenaries.
    /// Campaign / General read available units via <see cref="ITroopUnitRoster"/>.
    /// Supports runtime add + save/load so Recruitment is not a parallel army.
    /// </summary>
    public sealed class TemporaryAlphaTroopRoster : MonoBehaviour, ITroopUnitRoster
    {
        [SerializeField] private string _commanderId = "local-commander";

        private readonly List<TroopUnit> _allUnits = new List<TroopUnit>();
        private readonly HashSet<TroopUnit> _deployed = new HashSet<TroopUnit>();
        private bool _seeded;
        private bool _restoredFromSave;

        public string CommanderId => _commanderId;
        public int OwnedUnitCount => _allUnits.Count;

        private void Awake()
        {
            // CommanderProgressPersistence (execution order -40) may restore first.
            // Only seed when no save restore and the army is still empty.
            EnsureSeededIfEmpty();
        }

        private void Start()
        {
            // Late safety: if persistence was added after Awake in the same frame, still seed once.
            EnsureSeededIfEmpty();
        }

        public void SetCommanderId(string commanderId)
        {
            if (!string.IsNullOrWhiteSpace(commanderId))
                _commanderId = commanderId;
        }

        public IReadOnlyList<TroopUnit> GetAvailableUnits()
        {
            EnsureSeededIfEmpty();
            var available = new List<TroopUnit>(_allUnits.Count);
            for (int i = 0; i < _allUnits.Count; i++)
            {
                TroopUnit unit = _allUnits[i];
                if (unit != null && !_deployed.Contains(unit))
                    available.Add(unit);
            }

            return available;
        }

        public IReadOnlyList<TroopUnit> GetAllUnits()
        {
            EnsureSeededIfEmpty();
            return _allUnits;
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

        public bool TryAddUnit(TroopUnit unit)
        {
            if (unit == null)
                return false;

            EnsureSeededIfEmpty();
            if (_allUnits.Contains(unit))
                return true;

            _allUnits.Add(unit);
            return true;
        }

        /// <summary>
        /// Extra seed units for reinforcement testing (Support Commander).
        /// </summary>
        public TroopUnit CreateReinforcementUnit(string name, TroopUnitType type, int infantry, int cavalry, int archers)
        {
            return CreateAndAddUnit(name, type, infantry, cavalry, archers);
        }

        /// <summary>
        /// Phase 19: add a purchased mercenary into this commander's army.
        /// </summary>
        public TroopUnit CreateHiredUnit(string name, TroopUnitType type, int infantry, int cavalry, int archers)
        {
            return CreateAndAddUnit(name, type, infantry, cavalry, archers);
        }

        public void EnsureSeededIfEmpty()
        {
            if (_restoredFromSave || _seeded)
                return;

            if (_allUnits.Count > 0)
            {
                _seeded = true;
                return;
            }

            SeedDefaultUnits();
        }

        public void CaptureToSave(List<TroopUnitSaveEntry> destination)
        {
            if (destination == null)
                return;

            destination.Clear();
            for (int i = 0; i < _allUnits.Count; i++)
            {
                TroopUnit unit = _allUnits[i];
                if (unit == null)
                    continue;

                ArmyComposition comp = unit.DirectTroops;
                destination.Add(new TroopUnitSaveEntry
                {
                    name = unit.Name,
                    unitType = (int)unit.UnitType,
                    infantry = comp.Infantry,
                    cavalry = comp.Cavalry,
                    archers = comp.Archers,
                    deployed = _deployed.Contains(unit)
                });
            }
        }

        public void RestoreFromSave(IReadOnlyList<TroopUnitSaveEntry> entries)
        {
            _allUnits.Clear();
            _deployed.Clear();
            _seeded = true;
            _restoredFromSave = true;

            if (entries == null)
                return;

            for (int i = 0; i < entries.Count; i++)
            {
                TroopUnitSaveEntry entry = entries[i];
                if (entry == null)
                    continue;

                var type = (TroopUnitType)entry.unitType;
                var unit = new TroopUnit(type, entry.name);
                unit.TrySetDirectTroops(entry.infantry, entry.cavalry, entry.archers, out _);
                _allUnits.Add(unit);
                if (entry.deployed)
                    _deployed.Add(unit);
            }
        }

        private TroopUnit CreateAndAddUnit(string name, TroopUnitType type, int infantry, int cavalry, int archers)
        {
            EnsureSeededIfEmpty();
            var unit = new TroopUnit(type, name);
            if (!unit.TrySetDirectTroops(infantry, cavalry, archers, out string error))
            {
                Debug.LogWarning($"[TemporaryAlphaTroopRoster] Failed to create {name}: {error}");
                return null;
            }

            _allUnits.Add(unit);
            return unit;
        }

        private void SeedDefaultUnits()
        {
            _seeded = true;
            _allUnits.Clear();
            _deployed.Clear();

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
