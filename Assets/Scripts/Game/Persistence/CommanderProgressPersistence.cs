using Game.Currency;
using Game.Recruitment;
using Game.Troops;
using UnityEngine;

namespace Game.Persistence
{
    /// <summary>
    /// Loads/saves Gold, mercenary pool availability, and commander army into one local file.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class CommanderProgressPersistence : MonoBehaviour
    {
        [SerializeField] private CurrencySystem currency;
        [SerializeField] private MercenaryPool pool;
        [SerializeField] private TemporaryAlphaTroopRoster roster;
        [SerializeField] private string fileName = "commander_progress.json";
        [SerializeField] private bool loadOnAwake = true;
        [SerializeField] private bool saveOnQuit = true;

        private CommanderProgressRepository _repository;
        private bool _loaded;

        public string FilePath => _repository != null ? _repository.FilePath : string.Empty;
        public bool HasLoaded => _loaded;

        private void Awake()
        {
            _repository = new CommanderProgressRepository(fileName);
            ResolveRefs();
            if (loadOnAwake)
                LoadNow();
        }

        private void OnApplicationQuit()
        {
            if (saveOnQuit)
                SaveNow();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && saveOnQuit)
                SaveNow();
        }

        public void ResolveRefs()
        {
            if (currency == null)
                currency = FindFirstObjectByType<CurrencySystem>();
            if (pool == null)
                pool = GetComponent<MercenaryPool>() ?? FindFirstObjectByType<MercenaryPool>();
            if (roster == null)
                roster = FindFirstObjectByType<TemporaryAlphaTroopRoster>();
        }

        public void LoadNow()
        {
            ResolveRefs();
            pool?.EnsureInitialized();

            CommanderProgressSaveData data = _repository.Load();
            if (data == null)
            {
                // Fresh run: ensure currency starting balances if still zeroed and settings exist.
                if (currency != null && currency.Gold == 0 && currency.Silver == 0)
                    currency.ResetToStartingBalances();

                roster?.EnsureSeededIfEmpty();
                _loaded = true;
                Debug.Log("[Persistence] No save found — using defaults / seed roster.");
                return;
            }

            if (data.hasCurrency && currency != null)
                currency.ApplyLoadedBalances(data.gold, data.silver);

            if (data.hasPool && pool != null && data.pool != null)
                pool.ApplyAvailability(data.pool);

            if (data.hasArmy && roster != null && data.army != null)
                roster.RestoreFromSave(data.army);
            else
                roster?.EnsureSeededIfEmpty();

            _loaded = true;
            Debug.Log(
                $"[Persistence] Loaded progress. Gold={currency?.Gold}, " +
                $"Army={roster?.OwnedUnitCount}, file={_repository.FilePath}");
        }

        public void SaveNow()
        {
            ResolveRefs();
            if (_repository == null)
                _repository = new CommanderProgressRepository(fileName);

            var data = new CommanderProgressSaveData();

            if (currency != null)
            {
                data.hasCurrency = true;
                data.gold = currency.Gold;
                data.silver = currency.Silver;
            }

            if (pool != null)
            {
                data.hasPool = true;
                pool.CaptureAvailability(data.pool);
            }

            if (roster != null)
            {
                data.hasArmy = true;
                roster.CaptureToSave(data.army);
            }

            _repository.Save(data);
        }

        public void DeleteSave()
        {
            if (_repository == null)
                _repository = new CommanderProgressRepository(fileName);
            _repository.Delete();
        }
    }
}
