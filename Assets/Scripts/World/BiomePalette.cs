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

   public static BiomeType ColorToBiome(Color32 c, float threshold = 48f)
   {
      // Prefer nearest biome within threshold (colors often drift after materials/lighting).
      float best = float.MaxValue;
      BiomeType result = BiomeType.Grassland;

      Try(c, Lake, BiomeType.Lake, threshold, ref best, ref result);
      Try(c, Mountain, BiomeType.Mountain, threshold, ref best, ref result);
      Try(c, Forest, BiomeType.Forest, threshold, ref best, ref result);
      Try(c, Grassland, BiomeType.Grassland, threshold, ref best, ref result);
      return result;
   }

   private static void Try(
      Color32 sample,
      Color32 candidate,
      BiomeType biome,
      float threshold,
      ref float best,
      ref BiomeType result)
   {
      float d = TerrainColorPalette.ColorDistance(sample, candidate);
      if (d <= threshold && d < best)
      {
         best = d;
         result = biome;
      }
   }
}
