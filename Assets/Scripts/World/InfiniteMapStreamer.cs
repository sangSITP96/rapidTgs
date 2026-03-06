using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor.Rendering.LookDev;
using UnityEngine;

public class InfiniteMapStreamer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform _marble;
    [SerializeField] private GameObject _chunkPrefab;

    [Header("Chunk World Size")]
    [SerializeField] private Vector2 _tileSize = new Vector2(8.75f, 6.25f);

    [Header("Data Source")]
    [SerializeField] private MapChunkData _defaultData;
    [SerializeField] private List<MapChunkEntry> _predefined = new();

    [System.Serializable]
    public class MapChunkEntry
    {
        public Vector2Int coord;
        public MapChunkData data;
    }

    private readonly Dictionary<Vector2Int, MapChunkRuntime> _active = new();
    private readonly Dictionary<Vector2Int, MapChunkData> _dataMap = new();

    private Vector2Int _currentCoord;

    public bool TryGetChunkAtWorld(Vector3 worldPos, out MapChunkRuntime chunk)
    {
        var c = WorldToCoord(worldPos);
        return _active.TryGetValue(c, out chunk);
    }

    private void Awake()
    {
        foreach(var e in _predefined)
        {
            _dataMap[e.coord] = e.data;
        }
    }

    private void Start()
    {
        _currentCoord = WorldToCoord(_marble.position);
        Ensure3x3(_currentCoord);
    }

    private void Update()
    {

    }

    private void Ensure3x3(Vector2Int center)
    {
        for(int y = -1; y <= 1; y++)
        {
            for(int x = -1; x <= 1; x++)
            {
                var c = new Vector2Int(center.x + x, center.y + y);
                if (_active.ContainsKey(c)) continue;
                
                SpawnChunk(c);
            }
        }
    }

    private void SpawnChunk(Vector2Int coord)
    {

    }

    private Vector2Int WorldToCoord(Vector3 pos)
    {
        int coordX = Mathf.FloorToInt(pos.x / _tileSize.x); 
        int coordY = Mathf.FloorToInt(pos.z / _tileSize.y);
        
        return new Vector2Int(coordX, coordY);
    }

}
