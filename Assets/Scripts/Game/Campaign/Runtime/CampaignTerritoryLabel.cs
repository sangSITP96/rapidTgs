using TGS;

namespace Game.Campaign
{
    public static class CampaignTerritoryLabel
    {
        public static string GetDisplayName(int territoryIndex, TgsBiomeMapData biomeMapData)
        {
            BiomeType biome = biomeMapData != null
                ? biomeMapData.GetBiomeForTerritory(territoryIndex)
                : BiomeType.Grassland;

            return $"{biome} Territory";
        }

        public static string GetObjectivePlaceholder()
        {
            // Placeholder only — Capture Territory is not a full Objective System.
            return "Capture Territory";
        }
    }
}
