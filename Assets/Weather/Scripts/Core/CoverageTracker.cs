using System.Collections.Generic;
using Game.Weather.Cloud;
using Game.Weather.Storm;
using TGS;
using UnityEngine;

namespace Game.Weather.Core
{
    public class CoverageTracker : MonoBehaviour
    {
        [SerializeField] private TerrainGridSystem _tgs;
        [SerializeField] private CloudManager _cloudManager;
        [SerializeField] private StormLifecycleManager _stormManager;

        private const float MAX_COVERAGE_PERCENTAGE = 0.6f;

        public float GetCurrentCoverage()
        {
            if (_tgs == null || _tgs.cells == null || _tgs.cells.Count == 0)
            {
                return 0f;
            }

            HashSet<int> coveredCells = new();

            if (_cloudManager != null)
            {
                foreach (var cloud in _cloudManager.ActiveClouds)
                {
                    AddCoveredCells(cloud.Position, cloud.Radius, coveredCells);
                }
            }

            if (_stormManager != null)
            {
                foreach (var storm in _stormManager.ActiveStorm)
                {
                    AddCoveredCells(storm.Position, storm.Radius, coveredCells);
                }
            }
            return (float)coveredCells.Count/_tgs.cells.Count;
        }

        private void AddCoveredCells(Vector2 position, float radius, HashSet<int> coveredCells)
        {
            // Quick bounds check to skip far away cells
            Bounds checkBounds = new Bounds(
                new Vector3(position.x, 0, position.y),
                new Vector3(radius * 2, 1000, radius * 2)
            );
    
            foreach (Cell cell in _tgs.cells)
            {
                // Quick bounds rejection
                if (!cell.region.rect2D.Overlaps(new Rect(
                        position.x - radius, 
                        position.y - radius, 
                        radius * 2, 
                        radius * 2)))
                {
                    continue;
                }
        
                // Precise distance check
                Vector3 cellWorldPos = _tgs.CellGetPosition(cell, worldSpace: true);
                Vector2 cellPos2D = new Vector2(cellWorldPos.x, cellWorldPos.z);
        
                float distanceSqr = (position - cellPos2D).sqrMagnitude;
        
                if (distanceSqr <= radius * radius)
                {
                    coveredCells.Add(cell.index);
                }
            }
        }

        public bool CanSpawnNewCloud()
        {
            return GetCurrentCoverage() < MAX_COVERAGE_PERCENTAGE;
        }
    }
}