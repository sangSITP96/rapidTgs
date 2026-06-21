using System.IO;
using UnityEngine;

public class ColonyEventLogRepository
{
    private readonly string _filePath;
    public ColonyEventLogRepository(string fileName = "colony_event_log.json")
    {
        _filePath = Path.Combine(Application.persistentDataPath, fileName);
        Debug.Log($"file Path: {_filePath}");
    }

    public ColonyEventLogSaveData Load()
    {
        if(!File.Exists(_filePath))
        {
            return new ColonyEventLogSaveData();
        }
        try
        {
            string json = File.ReadAllText(_filePath);
            var data = JsonUtility.FromJson<ColonyEventLogSaveData>(json);

            return data ?? new ColonyEventLogSaveData();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[EventLog] Load failed: {ex.Message}");
            return new ColonyEventLogSaveData();
        }
    }

    public void Save(ColonyEventLogSaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: false);

            File.WriteAllText(_filePath, json);
        }
        catch(System.Exception ex)
        {
            Debug.LogError($"[EventLog] Save failed: {ex.Message}");
        }
    }
}
