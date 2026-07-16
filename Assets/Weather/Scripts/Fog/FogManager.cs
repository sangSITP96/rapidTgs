using Game.Core.WorldTime;
using Game.Weather.Cloud;
using Game.Weather.Lake;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Weather.Fog
{ 
    public class FogManager : MonoBehaviour
    {
        [SerializeField] private WorldTime _worldTime;
        [SerializeField] private WorldTick _fogSpawnTick;
        [SerializeField] private LakeDetector _lakeDetector;

        [Header("Config")]
        [Range(0f, 1f)]
        [SerializeField] private float _fogSpawnChance = 0.5f;

        [SerializeField] private bool _removeFogForMissingLakes = true;

        [Header("Visual")]
        [SerializeField] private FogVisualLibrary _fogVisualLibrary;
        [SerializeField] private Transform _fogVisualParent;

        [SerializeField] private float _fogVisualHeight = 0.2f;

        [SerializeField] private bool _applyCategoryScale = true;

        public IReadOnlyList<Fog> ActiveFogs => _fogs;

        private readonly List<Fog> _fogs = new();
        private readonly Dictionary<int, Fog> _fogBylakeId = new();

        private int _nextFogId;

        private void OnEnable()
        {
            if (_fogSpawnTick != null)
                _fogSpawnTick.OnTick += HandleSpawnTick;
        }

        private void OnDisable()
        {
            if (_fogSpawnTick != null)
                _fogSpawnTick.OnTick -= HandleSpawnTick;
        }

        private void HandleSpawnTick(long tickIndex, double gameTime)
        {
            if (_lakeDetector == null || _lakeDetector.Lakes == null) return;

            if (_removeFogForMissingLakes)
                RemoveFogForMissingLakes();

            foreach (var lake in _lakeDetector.Lakes)
            {
                if (_fogBylakeId.ContainsKey(lake.Id))
                    continue;

                if (Random.value < _fogSpawnChance)
                {
                    SpawnFogAtLake(lake);
                }
            }
        }

        private void SpawnFogAtLake(Lake.Lake lake)
        {
            if (float.IsNaN(lake.Center.x) || float.IsNaN(lake.Center.y))
            {
                Debug.LogError($"[Fog] Invalid lake center: {lake.Center} for lake {lake.Id}");
                return;
            }

            if (_fogVisualLibrary == null)
            {
                Debug.LogError($"[Fog] FogVisualLibrary is not assigned.");
                return;
            }

            if (!_fogVisualLibrary.TryPickPrefabForLake(
                lake.Size,
                out GameObject prefab,
                out FogVisualCategory category,
                out float scale))
            {
                Debug.LogWarning($"[Fog] No VFX prefab configured for lake size {lake.Size}");
                return;
            }

            Fog fog = new Fog
            {
                Id = _nextFogId++,
                SourceLakeId = lake.Id,
                Position = lake.Center,
                SourceLakeSize = lake.Size,
                VisualCategory = category
            };

            Vector3 spawnPos = GetFogWorldPosition(fog.Position);

            fog.VisualObject = Instantiate(
                 prefab,
                 spawnPos,
                 Quaternion.identity,
                 _fogVisualParent != null ? _fogVisualParent : transform);

            if (_applyCategoryScale)
                fog.VisualObject.transform.localScale = Vector3.one * scale;

            _fogs.Add(fog);
            _fogBylakeId[lake.Id] = fog;
        }

        private void RemoveFogForMissingLakes()
        {
            for (int i = _fogs.Count - 1; i >= 0; i--)
            {
                Fog fog = _fogs[i];

                if (LakeExists(fog.SourceLakeId))
                    continue;

                DestroyFog(fog, i);
            }
        }

        private bool LakeExists(int lakeId)
        {
            foreach (var lake in _lakeDetector.Lakes)
            {
                if(lake.Id == lakeId)
                    return true;
            }

            return false;
        }

        private void DestroyFog(Fog fog, int index)
        {
            if (fog.VisualObject != null)
                Destroy(fog.VisualObject);

            _fogBylakeId.Remove(fog.SourceLakeId);
            _fogs.RemoveAt(index);
        }

        public void RemoveFog(Fog fog)
        { 
            var idx = _fogs.IndexOf(fog);

            if (idx >= 0)
                DestroyFog(fog, idx);
        }

        public void ClearAllFog()
        {
            for (int i = _fogs.Count - 1; i >= 0; i--)
                DestroyFog(_fogs[i], i);
        }

        private Vector3 GetFogWorldPosition(Vector2 xzPosition)
        {
            return new Vector3(xzPosition.x, _fogVisualHeight, xzPosition.y);
        }
    }
}
