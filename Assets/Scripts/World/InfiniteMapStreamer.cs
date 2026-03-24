using System.Collections.Generic;
using UnityEngine;

public class InfiniteMapStreamer : MonoBehaviour
{
    private enum ChunkPivotMode
    {
        Center,
        BottomLeft
    }

    [SerializeField] private ChunkUVMap _uvMap;

    [Header("Refs")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private Transform _marble;
    [SerializeField] private GameObject _chunkPrefab;

    [Header("Grid Size (N x M)")]
    [SerializeField] private int _rows = 4;
    [SerializeField] private int _columns = 4;

    [Header("Chunk World Size")]
    [SerializeField] private Vector2 _tileSize = new Vector2(8.75f, 6.25f);
    [SerializeField] private float _spawnY = 0f;

    [Header("World Alignment")]
    [SerializeField] private ChunkPivotMode _pivotMode = ChunkPivotMode.Center;
    [SerializeField] private Vector2 _worldOrigin = Vector2.zero;

    [Header("Streaming Radius (int tile units)")]
    [SerializeField] private int _cameraRadiusX = 2;
    [SerializeField] private int _cameraRadiusY = 2;

    [SerializeField] private int _marbleRadiusX = 1;
    [SerializeField] private int _marbleRadiusY = 1;

    [SerializeField] private int _unloadMargin = 1;

    [SerializeField] private float _refreshInterval = 0.15f;

    public int Columns => _columns;
    public int Rows => _rows;

    [Header("Horizontal Streaming")]
    [SerializeField] private int _preloadLeftRight = 2;
    [SerializeField] private bool _useMarbleStartRow = true;
    [SerializeField] private int _forcedRow = 0;

    [Header("Data Source")]
    [SerializeField] private MapChunkData _defaultData;
    [SerializeField] private List<MapChunkEntry> _predefined = new();

    [System.Serializable]
    public class MapChunkEntry
    {
        public Vector2Int coord;
        public MapChunkData data;
    }

    private readonly Dictionary<Vector2Int, MapChunkRuntime> _loaded = new();
    private readonly Dictionary<Vector2Int, MapChunkData> _dataMap = new();

    private Vector2Int _currentCoord;

    private int _currentX;
    private int _activeRow;

    private float _nextRefreshTime;

    public bool TryGetChunkAtWorld(Vector3 worldPos, out MapChunkRuntime chunk)
    {
        var c = WorldToCoord(worldPos);
         
        if(!IsCoordValid(c))
        {
            chunk = null;
            return false;
        }

        return _loaded.TryGetValue(c, out chunk);
    }

    private void Awake()
    {
        _dataMap.Clear();

        foreach (var e in _predefined)
        {
            if (e.data != null)
            {
                _dataMap[e.coord] = e.data;
            }
        }
    }

    private void Start()
    {
        RefreshStreaming(force: true);
    }

    private void Update()
    {
        if (Time.time < _nextRefreshTime) return;

        _nextRefreshTime = Time.time + _refreshInterval;
        RefreshStreaming(force: false);
    }

    private void RefreshStreaming(bool force)
    {
        if (_rows <= 0 || _columns <= 0 || _chunkPrefab == null) return;
        if (_cameraTransform == null || _marble == null) return;

        Vector2Int camCoord = WorldToCoord(_cameraTransform.position);
        Vector2Int marbleCoord = WorldToCoord(_marble.position);

        var mustLoad = new HashSet<Vector2Int>();

        AddCoordsAround(camCoord, _cameraRadiusX, _cameraRadiusY, mustLoad);
        AddCoordsAround(marbleCoord, _marbleRadiusX, _marbleRadiusY, mustLoad);

        var keepAlive = new HashSet<Vector2Int>();

        AddCoordsAround(camCoord, _cameraRadiusX + _unloadMargin, _cameraRadiusY + _unloadMargin, keepAlive);
        AddCoordsAround(marbleCoord, _marbleRadiusX + _unloadMargin, _marbleRadiusY + _unloadMargin, keepAlive);

        foreach(var c in mustLoad)
        {
            if(!_loaded.ContainsKey(c))
            {
                SpawnChunk(c);
            }
        }

        var toUnload = new List<Vector2Int>();

        foreach(var kv in _loaded)
        {
            if(!keepAlive.Contains(kv.Key))
            {
                toUnload.Add(kv.Key);
            }
        }

        foreach(var c in toUnload)
        {
            if(_loaded.TryGetValue(c, out var runtime) && runtime != null)
            {
                Destroy(runtime.gameObject);
            }

            _loaded.Remove(c);
        }

        if(force)
        {
            Vector3 clamped = ClampWorldPositionXZ(_marble.position, 0.001f);
            _marble.position = clamped;
        }
    }

    private void AddCoordsAround(Vector2Int center, int radiusX, int radiusY, HashSet<Vector2Int> output)
    {
        for(int y = center.y - radiusY; y <= center.y + radiusY; y++)
        {
            for(int x = center.x - radiusX; x <= center.x + radiusX; x++)
            {
                Vector2Int c = new Vector2Int(x, y);

                if(IsCoordValid(c))
                {
                    output.Add(c);
                }
            }
        }
    }

    private bool IsCoordValid(Vector2Int c) 
    {
        return c.x >= 0 && c.x < _columns && c.y >= 0 && c.y < _rows;
    }

    private void SpawnChunk(Vector2Int coord)
    {
        Vector3 pos = CoordToWorld(coord);

        var go = Instantiate(_chunkPrefab, pos, Quaternion.Euler(90f, 0f, 0f), transform);
        go.transform.position = pos;

        var runtime = go.GetComponent<MapChunkRuntime>();

        if(runtime == null)
        {
            Destroy(go);
            return;
        }

        var rendererRef = go.GetComponentInChildren<Renderer>();

        var data = ResolveData(coord);

        runtime.Init(coord, data, rendererRef, _columns, _rows);
        _loaded[coord] = runtime;
    }

    private MapChunkData ResolveData(Vector2Int coord)
    {
        if(_dataMap.TryGetValue(coord, out var d) && d!=null)
        {
            return d;
        }

        UnityEngine.Debug.Log("uaaaa: " + coord + "====");
        return _defaultData;
    }

    private bool IsInsideWorld(Vector3 worldPos)
    {
        return TryGetWorldBounds(
                out float minX,
                out float maxX,
                out float minZ,
                out float maxZ) &&
                worldPos.x >= minX && worldPos.x <= maxX &&
                worldPos.z >= minZ && worldPos.z <= maxZ;
    }

    public Vector3 ClampWorldPositionXZ(Vector3 worldPos, float padding = 0f)
    {
        if(!TryGetWorldBounds(out float minX, out float maxX, out float minZ, out float maxZ))
        {
            return worldPos;
        }

        float x = Mathf.Clamp(worldPos.x, minX + padding, maxX - padding);
        float z = Mathf.Clamp(worldPos.z, minZ + padding, maxZ - padding);

        return new Vector3(x, worldPos.y, z);
    }

    public bool TryGetWorldBounds(
        out float minX,
        out float maxX,
        out float minZ,
        out float maxZ)
    {
        if(_rows <= 0 || _columns <= 0 || _tileSize.x <=0f || _tileSize.y <= 0f)
        {
            minX = maxX = minZ = maxZ = 0f; 
            return false;
        }

        minX = _worldOrigin.x;
        maxX = _worldOrigin.x + _columns*_tileSize.x;

        minZ = _worldOrigin.y;
        maxZ = _worldOrigin.y + _rows*_tileSize.y;

        return true;
    }

    private Vector2Int WorldToCoord(Vector3 pos)
    {
        float lx = pos.x - _worldOrigin.x;
        float lz = pos.z - _worldOrigin.y;

        int coordX = Mathf.FloorToInt(lx / _tileSize.x); 
        int coordY = Mathf.FloorToInt(lz / _tileSize.y);
        
        return new Vector2Int(coordX, coordY);
    }

    private Vector3 CoordToWorld(Vector2Int c)
    {
        float x;
        float z;

        if(_pivotMode == ChunkPivotMode.Center)
        {
            x = _worldOrigin.x + (c.x +0.5f)*_tileSize.x;
            z = _worldOrigin.y + (c.y + 0.5f) * _tileSize.y;
        }
        else
        {
            x = _worldOrigin.x + c.x * _tileSize.x;
            z = _worldOrigin.y +c.y * _tileSize.y;
        }

        // smooth snap
        x = Mathf.Round(x * 10000f) / 10000f;
        z = Mathf.Round(z * 10000f) / 10000f;

        return new Vector3(x, _spawnY, z);
    }
}
