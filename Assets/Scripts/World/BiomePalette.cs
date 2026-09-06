using UnityEngine;

public static class BiomePalette
{
   public static readonly Color32 Lake = new Color32(0, 100, 255, 255);
   public static readonly Color32 Mountain = new Color32(139, 69, 19, 255);
   public static readonly Color32 Grassland = new Color32(144, 238, 144, 255);
   public static readonly Color32 Forest = new Color32(0, 100, 0, 255);

   public static Color32 GetColor(BiomeType biome)
   {
      switch (biome)
      {
         case BiomeType.Lake: return Lake;
         case BiomeType.Mountain: return Mountain;
         case BiomeType.Forest: return Forest;
         default: return Grassland;
      }
   }

   public static BiomeType ColorToBiome(Color32 c, float threshold = 5f)
   {
      if(TerrainColorPalette.ColorDistance(c, Lake) <= threshold) return BiomeType.Lake;
      if(TerrainColorPalette.ColorDistance(c, Mountain) <= threshold) return BiomeType.Mountain;
      if(TerrainColorPalette.ColorDistance(c, Forest) <= threshold) return BiomeType.Forest;
      if(TerrainColorPalette.ColorDistance(c, Grassland) <= threshold) return BiomeType.Grassland;
      return BiomeType.Grassland;
   }
}
