using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ColonyEventLogService : MonoBehaviour
{
    public static ColonyEventLogService Instance { get; private set; }

    [SerializeField]
    private int _maxEvents = 100;

    private ColonyEventLogRepository _repository;
    private ColonyEventLogSaveData _data;

    public IReadOnlyList<ColonyEventEntry> Events => _data.Events;

    public event Action OnLogChanged;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _repository = new ColonyEventLogRepository();
        _data = _repository.Load();
        Debug.Log("Init ColonyEventLog Service complete");
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    private void Save()
    {
        _repository?.Save(_data);
    }

    public void Add(ColonyEventEntry entry)
    {
        if(entry == null)
        {
            return;
        }
        Debug.Log("call Add from ColonyEventLogService");
        _data.Events.Add(entry);

        TrimIfNeeded();

        OnLogChanged?.Invoke();
        Save();
    }

    public void AddSimple(
        EventCategory category,
        string title,
        string summary = "",
        string payloadJson = "")
    {
        Debug.Log("call AddSimple: ");
        Add(ColonyEventEntry.Create(category, title, summary, payloadJson));
    }

    private void TrimIfNeeded()
    {
        if(_data.Events.Count <= _maxEvents)
        {
            return;
        }

        int remove = _data.Events.Count - _maxEvents;

        _data.Events.RemoveRange(0, remove);
    }

    // Ready for Filter Query
    public IEnumerable<ColonyEventEntry>Query(
        EventCategory? category = null,
        long? fromUnixMsUtc = null,
        long? toUnixMsUtc = null)
    {
        IEnumerable<ColonyEventEntry> q = _data.Events;

        if(category.HasValue)
        {
            q = q.Where(e => e.Category == category.Value);
        }

        if(fromUnixMsUtc.HasValue)
        {
            q = q.Where(e => e.TimestampUnixMsUtc >= fromUnixMsUtc.Value);
        }

        if(toUnixMsUtc.HasValue)
        {
            q = q.Where(e => e.TimestampUnixMsUtc <= toUnixMsUtc.Value);
        }

        return q.OrderByDescending(e => e.TimestampUnixMsUtc);
    }

    public void ClearAll()
    {
        _data.Events.Clear();

        OnLogChanged?.Invoke();
        Save();
    }
}
