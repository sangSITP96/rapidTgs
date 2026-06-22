using System.Collections.Generic;
using Game.Utilities;
using TGS;
using UnityEngine;

namespace Game.Weather.Lake
{
    public class LakeDetector : MonoBehaviour
    {
        [SerializeField] private TerrainGridSystem _tgs;
        [SerializeField] private TerrainMaskUtility _terrainMaskUtility;

        private readonly List<Lake> _lakes = new();
        public IReadOnlyList<Lake> Lakes => _lakes;

        private void Awake()
        {
            if (_terrainMaskUtility == null)
            {
                _terrainMaskUtility = TerrainMaskUtility.Instance;
            }
        }
        
        private void Start()
        {
            DetectLakes();
        }
        
        public void DetectLakes()
        {
            if (_terrainMaskUtility == null)
            {
                Debug.LogError("LakeDetector: TerrainMaskUtility is not assigned or found in scene.");
                return;
            }

            _lakes.Clear();
            DetectFromTerrainMask();
        }

        private void DetectFromTerrainMask()
        {
            HashSet<int> visited = new HashSet<int>();
            int lakeId = 0;

            Debug.Log((_tgs != null));
            foreach (Cell cell in _tgs.cells)
            {
                if (IsWaterCell(cell) && !visited.Contains(cell.index))
                {
                    Lake lake = FloodFillLake(cell.index, visited, lakeId++);
                    if (lake.CellIndies.Count >= 3) // Minimum 3 cells to be considered a lake
                    {
                        _lakes.Add(lake);
                    }
                }
            }

            foreach (var lake in _lakes)
            {
                Debug.Log($"[Lake] id={lake.Id}, size={lake.Size}, center={lake.Center}");
            }
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
                
                // Get world position for center calculation
                Vector3 worldPos = _tgs.CellGetPosition(cellIndex, worldSpace: true);
                
                // ADD: Validate position
                if (float.IsNaN(worldPos.x) || float.IsNaN(worldPos.z))
                {
                    Debug.LogError($"LakeDetector: Invalid cell position at index {cellIndex}");
                    continue;
                }
                
                centerSum += new Vector2(worldPos.x, worldPos.z);

                foreach (Cell neighbor in cell.neighbours)
                {
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
            }
            lake.Size = lake.CellIndies.Count;
            
            return lake;
        }

        private bool IsWaterCell(Cell cell)
        {
            if (_terrainMaskUtility == null)
            {
                return false;
            }

            // Get world position of cell center
            Vector3 worldPos = _tgs.CellGetPosition(cell, worldSpace: true);
            
            // Use terrain mask utility to check if it's lake
            return _terrainMaskUtility.IsLake(worldPos);
        }
    } 
}