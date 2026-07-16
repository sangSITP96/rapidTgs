using Game.Weather.Cloud;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Weather.Fog
{
    [Serializable]
    public class FogVisualVariantSet
    { 
        public FogVisualCategory Category;

        public List<GameObject> Prefabs = new();

        public float MinScale = 1f;
        public float MaxScale = 1.5f;
    }

    [CreateAssetMenu(menuName = "Game/Weather/Fog Visual Library", fileName = "FogVisualLibrary")]
    public class FogVisualLibrary : ScriptableObject
    {
        [Header("Lake Size Threshold")]
        [Min(1f)] public int MediumMinLakeSize = 6;

        [Min(1f)] public int LargeMinLakeSize = 15;

        [Header("Variants")]
        public List<FogVisualVariantSet> VariantSets = new();

        public FogVisualCategory GetCategoryForLakeSize(float lakeSize)
        {
            if (lakeSize >= LargeMinLakeSize) return FogVisualCategory.Large;
            if (lakeSize >= MediumMinLakeSize) return FogVisualCategory.Medium;

            return FogVisualCategory.Small;
        }

        public bool TryPickPrefab(
            FogVisualCategory category,
            out GameObject prefab,
            out float scale)
        {
            prefab = null;
            scale = 1f;

            FogVisualVariantSet set = FindSet(category);
            if (set == null || set.Prefabs == null || set.Prefabs.Count == 0)
                return false;

            prefab = set.Prefabs[UnityEngine.Random.Range(0, set.Prefabs.Count)];
            scale = UnityEngine.Random.Range(set.MinScale, set.MaxScale);

            return prefab != null;
        }

        public bool TryPickPrefabForLake(
            float lakeSize,
            out GameObject prefab,
            out FogVisualCategory category,
            out float scale)
        {
            category = GetCategoryForLakeSize(lakeSize);
            return TryPickPrefab(category, out prefab, out scale);
        }

        private FogVisualVariantSet FindSet(FogVisualCategory category)
        {
            foreach (var set in VariantSets)
            {
                if (set != null && set.Category == category)
                    return set;
            }

            return null;
        }

    }
}


