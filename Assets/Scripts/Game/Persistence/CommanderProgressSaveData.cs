using System;
using System.Collections.Generic;

namespace Game.Persistence
{
    /// <summary>
    /// Single local progress blob for Phase 19: Gold/Silver, mercenary pool stock, commander army.
    /// Follows the same JsonUtility + persistentDataPath pattern as ColonyEventLog.
    /// </summary>
    [Serializable]
    public sealed class CommanderProgressSaveData
    {
        public int gold;
        public int silver;
        public List<MercenaryAvailabilitySaveEntry> pool = new List<MercenaryAvailabilitySaveEntry>();
        public List<TroopUnitSaveEntry> army = new List<TroopUnitSaveEntry>();
        public bool hasCurrency;
        public bool hasPool;
        public bool hasArmy;
    }

    [Serializable]
    public sealed class MercenaryAvailabilitySaveEntry
    {
        public string offerId;
        public int available;
    }

    [Serializable]
    public sealed class TroopUnitSaveEntry
    {
        public string name;
        public int unitType;
        public int infantry;
        public int cavalry;
        public int archers;
        public bool deployed;
    }
}
