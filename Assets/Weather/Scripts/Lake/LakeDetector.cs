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

        [Header("Chunk Scan")]
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

            if (_onlyScanLoadedChunks && _mapStreamer == null)
            {
                Debug.LogWarning("LakeDetector: Map streamer not found; scanning all TGS cells.");
            }

            _lakes.Clear();
            DetectFromTerrainMask();
        }

        private void DetectFromTerrainMask()
        {
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
                    {
                        _lakes.Add(lake);
                    }
                }
            }

            foreach (var lake in _lakes)
            {
                Debug.Log($"[Lake] id={lake.Id}, chunk={lake.SourceChunkCoord}, size={lake.Size}, center={lake.Center}");
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
                Debug.LogError($"Lake {lakeId} has no cells!");
                lake.Center = Vector2.zero;
            }
            else
            {
                lake.Center = centerSum / lake.CellIndies.Count;

                if (_mapStreamer != null)
                    lake.SourceChunkCoord = _mapStreamer.WorldToChunkCoord(
                        new Vector3(lake.Center.x, 0f, lake.Center.y));
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
