using System;
using System.IO;
using UnityEngine;

namespace Game.Persistence
{
    /// <summary>
    /// Local JSON save for commander progress (currency, recruitment pool, army).
    /// Mirrors <see cref="ColonyEventLogRepository"/> style — no networking.
    /// </summary>
    public sealed class CommanderProgressRepository
    {
        private readonly string _filePath;

        public string FilePath => _filePath;

        public CommanderProgressRepository(string fileName = "commander_progress.json")
        {
            _filePath = Path.Combine(Application.persistentDataPath, fileName);
        }

        public bool Exists()
        {
            return File.Exists(_filePath);
        }

        public CommanderProgressSaveData Load()
        {
            if (!File.Exists(_filePath))
                return null;

            try
            {
                string json = File.ReadAllText(_filePath);
                return JsonUtility.FromJson<CommanderProgressSaveData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Persistence] Load failed: {ex.Message}");
                return null;
            }
        }

        public void Save(CommanderProgressSaveData data)
        {
            if (data == null)
                return;

            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(_filePath, json);
                Debug.Log($"[Persistence] Saved commander progress → {_filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Persistence] Save failed: {ex.Message}");
            }
        }

        public void Delete()
        {
            try
            {
                if (File.Exists(_filePath))
                    File.Delete(_filePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Persistence] Delete failed: {ex.Message}");
            }
        }
    }
}
