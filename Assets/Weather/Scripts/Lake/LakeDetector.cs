using System.Collections.Generic;
using Game.Utilities;
using TGS;
using UnityEngine;

namespace Game.Weather.Lake
{
    [DefaultExecutionOrder(50)]
    public class LakeDetector : MonoBehaviour
    {
        [SerializeField] private TerrainGridSystem _tgs;
        [SerializeField] private WorldTerrainQuery _worldTerrainQuery;
        [SerializeField] private InfiniteMapStreamer _mapStreamer;
        [SerializeField] private TerrainMaskUtility _terrainMaskUtility;

        [Header("Detection")]
        [SerializeField] private bool _useChunkMaskScan = true;
        [SerializeField] private int _maskScanResolution = 48;
        [SerializeField] private int _minimumLakeMaskPixels = 2;

        [Header("TGS Fallback")]
        [SerializeField] private bool _onlyScanLoadedChunks = true;
        [SerializeField] private bool _rescanWhenChunksChange = true;
        [SerializeField] private int _minimumLakeCells = 3;

        private readonly List<Lake> _lakes = new();
        public IReadOnlyList<Lake> Lakes => _lakes;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (_mapStreamer != null)
                _mapStreamer.LoadedChunksChanged += HandleLoadedChunksChanged;
        }

        private void OnDisable()
        {
            if (_mapStreamer != null)
                _mapStreamer.LoadedChunksChanged -= HandleLoadedChunksChanged;
        }

        private void Start()
        {
            DetectLakes();
        }

        private void ResolveReferences()
        {
            if (_worldTerrainQuery == null)
                _worldTerrainQuery = FindFirstObjectByType<WorldTerrainQuery>();

            if (_mapStreamer == null)
                _mapStreamer = FindFirstObjectByType<InfiniteMapStreamer>();

            if (_terrainMaskUtility == null)
                _terrainMaskUtility = TerrainMaskUtility.Instance;
        }

        private void HandleLoadedChunksChanged()
        {
            if (!_rescanWhenChunksChange)
                return;

            DetectLakes();
        }

        public void DetectLakes()
        {
            ResolveReferences();

            if (_useChunkMaskScan && _worldTerrainQuery != null && _mapStreamer != null)
            {
                _lakes.Clear();
                DetectFromChunkMasks();
                return;
            }

            if (_tgs == null)
            {
                Debug.LogError("LakeDetector: TerrainGridSystem is not assigned.");
                return;
            }

            if (_worldTerrainQuery == null && _terrainMaskUtility == null)
            {
                Debug.LogError("LakeDetector: WorldTerrainQuery or TerrainMaskUtility is required.");
                return;
            }

            _lakes.Clear();
            DetectFromTerrainGrid();
        }

        private void DetectFromChunkMasks()
        {
            int lakeId = 0;
            int resolution = Mathf.Max(8, _maskScanResolution);

            foreach (var coord in _mapStreamer.LoadedChunkCoords)
            {
                if (!_mapStreamer.TryGetChunkWorldBounds(
                        coord,
                        out float minX,
                        out float maxX,
                        out float minZ,
                        out float maxZ))
                {
                    continue;
                }

                var isLake = new bool[resolution, resolution];
                int lakePixelCount = 0;

                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        var localUV = new Vector2(
                            resolution <= 1 ? 0.5f : x / (float)(resolution - 1),
                            resolution <= 1 ? 0.5f : y / (float)(resolution - 1));

                        bool lake = _worldTerrainQuery.IsLakeAtChunk(coord, localUV);
                        isLake[x, y] = lake;
                        if (lake)
                            lakePixelCount++;
                    }
                }

                if (lakePixelCount < _minimumLakeMaskPixels)
                    continue;

                var visited = new bool[resolution, resolution];

                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        if (!isLake[x, y] || visited[x, y])
                            continue;

                        Lake lake = FloodFillMaskLake(
                            coord,
                            isLake,
                            visited,
                            resolution,
                            x,
                            y,
                            minX,
                            maxX,
                            minZ,
                            maxZ,
                            lakeId++);

                        if (lake.Size >= _minimumLakeMaskPixels)
                            _lakes.Add(lake);
                    }
                }
            }

            foreach (var lake in _lakes)
            {
                Debug.Log(
                    $"[Lake] id={lake.Id}, chunk={lake.SourceChunkCoord}, size={lake.Size}, center={lake.Center}");
            }
        }

        private Lake FloodFillMaskLake(
            Vector2Int chunkCoord,
            bool[,] isLake,
            bool[,] visited,
            int resolution,
            int startX,
            int startY,
            float minX,
            float maxX,
            float minZ,
            float maxZ,
            int lakeId)
        {
            var lake = new Lake(lakeId)
            {
                SourceChunkCoord = chunkCoord
            };

            var queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(startX, startY));
            visited[startX, startY] = true;

            Vector2 centerSum = Vector2.zero;

            while (queue.Count > 0)
            {
                Vector2Int p = queue.Dequeue();
                lake.Size += 1f;

                float u = resolution <= 1 ? 0.5f : p.x / (float)(resolution - 1);
                float v = resolution <= 1 ? 0.5f : p.y / (float)(resolution - 1);
                float worldX = Mathf.Lerp(minX, maxX, u);
                float worldZ = Mathf.Lerp(minZ, maxZ, v);
                centerSum += new Vector2(worldX, worldZ);

                TryEnqueueLakeNeighbor(isLake, visited, resolution, queue, p.x + 1, p.y);
                TryEnqueueLakeNeighbor(isLake, visited, resolution, queue, p.x - 1, p.y);
                TryEnqueueLakeNeighbor(isLake, visited, resolution, queue, p.x, p.y + 1);
                TryEnqueueLakeNeighbor(isLake, visited, resolution, queue, p.x, p.y - 1);
            }

            if (lake.Size > 0f)
                lake.Center = centerSum / lake.Size;

            return lake;
        }

        private static void TryEnqueueLakeNeighbor(
            bool[,] isLake,
            bool[,] visited,
            int resolution,
            Queue<Vector2Int> queue,
            int x,
            int y)
        {
            if (x < 0 || y < 0 || x >= resolution || y >= resolution)
                return;

            if (!isLake[x, y] || visited[x, y])
                return;

            visited[x, y] = true;
            queue.Enqueue(new Vector2Int(x, y));
        }

        private void DetectFromTerrainGrid()
        {
            if (_onlyScanLoadedChunks && _mapStreamer == null)
                Debug.LogWarning("LakeDetector: Map streamer not found; scanning all TGS cells.");

            HashSet<int> visited = new HashSet<int>();
            int lakeId = 0;

            foreach (Cell cell in _tgs.cells)
            {
                if (!IsCellInActiveChunk(cell))
                    continue;

                if (IsWaterCell(cell) && !visited.Contains(cell.index))
                {
                    Lake lake = FloodFillLake(cell.index, visited, lakeId++);
                    if (lake.CellIndies.Count >= _minimumLakeCells)
                        _lakes.Add(lake);
                }
            }

            foreach (var lake in _lakes)
            {
                Debug.Log(
                    $"[Lake] id={lake.Id}, chunk={lake.SourceChunkCoord}, size={lake.Size}, center={lake.Center}");
            }
        }

        private bool IsCellInActiveChunk(Cell cell)
        {
            if (!_onlyScanLoadedChunks || _mapStreamer == null)
                return true;

            Vector3 worldPos = _tgs.CellGetPosition(cell, worldSpace: true);
            Vector2Int chunkCoord = _mapStreamer.WorldToChunkCoord(worldPos);
            return _mapStreamer.IsChunkLoaded(chunkCoord);
        }

        private Lake FloodFillLake(int startIndex, HashSet<int> visited, int lakeId)
        {
            Lake lake = new Lake(id: lakeId);
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(startIndex);
            visited.Add(startIndex);

            Vector2 centerSum = Vector2.zero;

            while (queue.Count > 0)
            {
                int cellIndex = queue.Dequeue();
                Cell cell = _tgs.cells[cellIndex];
                lake.CellIndies.Add(cellIndex);

                Vector3 worldPos = _tgs.CellGetPosition(cellIndex, worldSpace: true);

                if (float.IsNaN(worldPos.x) || float.IsNaN(worldPos.z))
                {
                    Debug.LogError($"LakeDetector: Invalid cell position at index {cellIndex}");
                    continue;
                }

                centerSum += new Vector2(worldPos.x, worldPos.z);

                foreach (Cell neighbor in cell.neighbours)
                {
                    if (!IsCellInActiveChunk(neighbor))
                        continue;

                    if (IsWaterCell(neighbor) && !visited.Contains(neighbor.index))
                    {
                        visited.Add(neighbor.index);
                        queue.Enqueue(neighbor.index);
                    }
                }
            }

            if (lake.CellIndies.Count == 0)
            {
                lake.Center = Vector2.zero;
            }
            else
            {
                lake.Center = centerSum / lake.CellIndies.Count;

                if (_mapStreamer != null)
                {
                    lake.SourceChunkCoord = _mapStreamer.WorldToChunkCoord(
                        new Vector3(lake.Center.x, 0f, lake.Center.y));
                }
            }

            lake.Size = lake.CellIndies.Count;
            return lake;
        }

        private bool IsWaterCell(Cell cell)
        {
            Vector3 worldPos = _tgs.CellGetPosition(cell, worldSpace: true);

            if (_worldTerrainQuery != null)
                return _worldTerrainQuery.IsLake(worldPos);

            if (_terrainMaskUtility != null)
                return _terrainMaskUtility.IsLake(worldPos);

            return false;
        }
    }
}
