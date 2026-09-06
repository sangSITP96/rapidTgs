using System;
using UnityEngine;

[CreateAssetMenu(menuName = "World/TGS Biome Map Data")]
public class TgsBiomeMapData : ScriptableObject
{
   [Serializable]
   public struct  TerritoryBiomeEntry
   {
      public int TerritoryIndex;
      public BiomeType Biome;
   }

   public int Seed = 1;
   public int TerritoryCount;
   public TerritoryBiomeEntry[] Entries = Array.Empty<TerritoryBiomeEntry>();

   public BiomeType GetBiomeForTerritory(int territoryIndex)
   {
      if(Entries == null) return BiomeType.Grassland;

      for (int i = 0; i < Entries.Length; i++)
      {
         if(Entries[i].TerritoryIndex == territoryIndex) 
            return Entries[i].Biome;
      }
      
      return BiomeType.Grassland;
   }
}
