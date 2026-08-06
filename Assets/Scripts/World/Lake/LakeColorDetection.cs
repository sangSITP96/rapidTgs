using UnityEngine;

/// <summary>
/// Compatibility facade over <see cref="TerrainColorDetection"/> for existing Lake bake callers.
/// </summary>
public static class LakeColorDetection
{
    public struct DetectionSettings
    {
        public int MinLakePixels;
        public int BigLakePixelThreshold;
        public bool ConnectDiagonals;

        public static DetectionSettings Default => new DetectionSettings
        {
            MinLakePixels = 64,
            BigLakePixelThreshold = 400,
            ConnectDiagonals = false
        };

        public TerrainColorDetection.DetectionSettings ToTerrainSettings()
        {
            return new TerrainColorDetection.DetectionSettings
            {
                FeatureType = TerrainFeatureType.Lake,
                MinRegionPixels = MinLakePixels,
                BigRegionPixelThreshold = BigLakePixelThreshold,
                ConnectDiagonals = ConnectDiagonals
            };
        }
    }

    public struct DetectionResult
    {
        public BakedLakeChunkData Data;

        public int CandidateRegionCount;
        public int AcceptedRegionCount;
        public int RejectedSmallRegionCount;
        public int PotentialLakePixelCount;

        public static DetectionResult FromTerrain(TerrainColorDetection.DetectionResult result)
        {
            return new DetectionResult
            {
                Data = result.Data,
                CandidateRegionCount = result.CandidateRegionCount,
                AcceptedRegionCount = result.AcceptedRegionCount,
                RejectedSmallRegionCount = result.RejectedSmallRegionCount,
                PotentialLakePixelCount = result.PotentialPixelCount
            };
        }
    }

    public static LakeColorPalette SamplePaletteFromReference(
        Texture2D reference,
        Color32 goldenMarkerColor,
        int goldenTolerance,
        int sampleRadius,
        float colorDistanceThreshold)
    {
        TerrainColorPalette palette = TerrainColorDetection.SamplePaletteFromReference(
            reference,
            goldenMarkerColor,
            goldenTolerance,
            sampleRadius,
            colorDistanceThreshold);

        return palette.ToLakePalette();
    }

    public static DetectionResult DetectAndBake(
        Texture2D visual,
        LakeColorPalette palette,
        DetectionSettings settings)
    {
        TerrainColorDetection.DetectionResult result = TerrainColorDetection.DetectAndBake(
            visual,
            TerrainColorPalette.FromLakePalette(palette),
            settings.ToTerrainSettings());

        return DetectionResult.FromTerrain(result);
    }

    public static Texture2D BuildPreviewMask(
        BakedLakeChunkData data,
        Color lakeColor,
        Color emptyColor)
    {
        return TerrainColorDetection.BuildPreviewMask(data, lakeColor, emptyColor);
    }
}
