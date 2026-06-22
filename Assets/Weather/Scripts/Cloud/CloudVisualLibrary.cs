using Game.Weather.Lake;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Weather.Cloud
{
    [Serializable]
    public class CloudVisualVariantSet
    {
        public CloudVisualCategory Category;

        [Tooltip("Prefab VFX of artist for this category")]
        public List<GameObject> Prefabs = new();

        [Tooltip("Radius simulation when spawn cloud")]
        public float MinSimulationRadius = 1f;
        public float MaxSimulationRadius = 1.5f;
    }

    [CreateAssetMenu(
        menuName = "Game/Weather/Cloud Visual Library",
        fileName = "CloudVisualLibrary")]
    public class CloudVisualLibrary : ScriptableObject
    {
        [Header("Lake Side Thresholds (TGS cell count)")]
        [Tooltip("Size < MediumMin => Small")]
        [Min(1)] public int MediumMinLakeSize = 6;

        [Tooltip("Size < LargeMin => Medium")]
        [Min(1)] public int LargeMinLakeSize = 15;

        [Tooltip("Size < ClusterMin => Large, else Cluster")]
        [Min(1)] public int ClusterMinLakeSize = 30;

        [Header("Variants")]
        public List<CloudVisualVariantSet> VariantSets = new();

        public CloudVisualCategory GetCategoryForLakeSize(float lakeSize)
        {
            if (lakeSize >= ClusterMinLakeSize) return CloudVisualCategory.Cluster;
            if (lakeSize >= LargeMinLakeSize) return CloudVisualCategory.Large;
            if (lakeSize >= MediumMinLakeSize) return CloudVisualCategory.Medium;

            return CloudVisualCategory.Small;
        }

        public bool TryPickPrefab(
            CloudVisualCategory category,
            out GameObject prefab,
            out float simulationRadius)
        {
            prefab = null;
            simulationRadius = 1f;

            CloudVisualVariantSet set = FindSet(category);
            if (set == null || set.Prefabs == null || set.Prefabs.Count == 0)
                return false;

            prefab = set.Prefabs[UnityEngine.Random.Range(0, set.Prefabs.Count)];
            simulationRadius = UnityEngine.Random.Range(set.MinSimulationRadius, set.MaxSimulationRadius);

            return prefab != null;
        }

        public bool TryPickPrefabForLake(
            float lakeSize,
            out GameObject prefab,
            out CloudVisualCategory category,
            out float simulationRadius)
        {
            category = GetCategoryForLakeSize(lakeSize);
            return TryPickPrefab(category, out prefab, out simulationRadius);
        }

        private CloudVisualVariantSet FindSet(CloudVisualCategory category)
        {
            foreach(var set in VariantSets)
            {
                if (set != null && set.Category == category)
                    return set;
            }

            return null;
        }
    }
}