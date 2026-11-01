using System;
using TGS;
using UnityEngine;

[CreateAssetMenu(menuName = "World/TGS Biome Map Data")]
public class TgsBiomeMapData : ScriptableObject
{
   [Serializable]
   public struct TerritoryBiomeEntry
   {
      public int TerritoryIndex;
      public BiomeType Biome;
   }

   public int Seed = 1;
   public int TerritoryCount;
   public TerritoryBiomeEntry[] Entries = Array.Empty<TerritoryBiomeEntry>();

   public bool HasEntries => Entries != null && Entries.Length > 0;

   public BiomeType GetBiomeForTerritory(int territoryIndex)
   {
      if (Entries == null)
         return BiomeType.Grassland;

      for (int i = 0; i < Entries.Length; i++)
      {
         if (Entries[i].TerritoryIndex == territoryIndex)
            return Entries[i].Biome;
      }

      return BiomeType.Grassland;
   }

   /// <summary>
   /// Rebuild territory→biome when the asset is empty.
   /// Prefers sampling the visual map at each territory centroid; falls back to TGS fill colors.
   /// </summary>
   public bool TryRebuildFromTerritoryColors(TerrainGridSystem tgs, float colorThreshold = 48f)
   {
      if (tgs == null || tgs.territories == null || tgs.territories.Count == 0)
         return false;

      int count = tgs.territories.Count;
      var entries = new TerritoryBiomeEntry[count];
      Color[] scheme = tgs.territoriesFillColors;
      var terrainQuery = UnityEngine.Object.FindFirstObjectByType<WorldTerrainQuery>();

      int nonGrassland = 0;
      for (int t = 0; t < count; t++)
      {
         BiomeType biome = BiomeType.Grassland;
         BiomeType sampled = BiomeType.Grassland;

         Vector3 centroid = tgs.TerritoryGetPosition(t, worldSpace: true);
         if (terrainQuery != null && terrainQuery.TryGetBiomeType(centroid, out sampled))
         {
            biome = sampled;
         }
         else
         {
            Color fill = Color.clear;
            if (scheme != null && t < scheme.Length)
               fill = scheme[t];
            else if (tgs.territories[t] != null)
               fill = tgs.territories[t].fillColor;

            biome = BiomePalette.ColorToBiome(fill, colorThreshold);
         }

         if (biome != BiomeType.Grassland)
            nonGrassland++;

         entries[t] = new TerritoryBiomeEntry
         {
            TerritoryIndex = t,
            Biome = biome
         };
      }

      Entries = entries;
      TerritoryCount = count;
      Debug.Log(
         $"[TgsBiomeMapData] Rebuilt {count} territories from map/colors " +
         $"({nonGrassland} non-Grassland).");
      return true;
   }
}
