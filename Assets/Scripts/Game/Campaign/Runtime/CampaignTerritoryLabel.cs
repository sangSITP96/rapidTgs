using TGS;
using UnityEngine;

namespace Game.Campaign
{
    public static class CampaignTerritoryLabel
    {
        public static string GetDisplayName(int territoryIndex, TgsBiomeMapData biomeMapData)
        {
            BiomeType biome = ResolveBiome(territoryIndex, biomeMapData);
            return $"{biome} Territory";
        }

        public static BiomeType ResolveBiome(int territoryIndex, TgsBiomeMapData biomeMapData)
        {
            var grid = UnityEngine.Object.FindFirstObjectByType<TerrainGridSystem>();

            if (biomeMapData != null)
            {
                if (!biomeMapData.HasEntries && grid != null)
                    biomeMapData.TryRebuildFromTerritoryColors(grid);

                if (biomeMapData.HasEntries)
                    return biomeMapData.GetBiomeForTerritory(territoryIndex);
            }

            if (grid != null &&
                grid.territories != null &&
                territoryIndex >= 0 &&
                territoryIndex < grid.territories.Count)
            {
                // Prefer what the player sees on the visual map.
                var terrainQuery = UnityEngine.Object.FindFirstObjectByType<WorldTerrainQuery>();
                if (terrainQuery != null)
                {
                    Vector3 centroid = grid.TerritoryGetPosition(territoryIndex, worldSpace: true);
                    BiomeType sampled = BiomeType.Grassland;
                    if (terrainQuery.TryGetBiomeType(centroid, out sampled))
                        return sampled;
                }

                Color fill = grid.territoriesFillColors != null &&
                             territoryIndex < grid.territoriesFillColors.Length
                    ? grid.territoriesFillColors[territoryIndex]
                    : grid.territories[territoryIndex].fillColor;
                return BiomePalette.ColorToBiome(fill, 48f);
            }

            return BiomeType.Grassland;
        }

        public static string GetObjectivePlaceholder()
        {
            // Placeholder only — Capture Territory is not a full Objective System.
            return "Capture Territory";
        }
    }
}
