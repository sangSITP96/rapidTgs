using UnityEngine;

[CreateAssetMenu(
    fileName = "TerrainDetectionConfig",
    menuName = "World/Terrain Detection Config")]
public class TerrainDetectionConfig : ScriptableObject
{
    [Header("Type")]
    public TerrainFeatureType FeatureType = TerrainFeatureType.Lake;

    [Header("Golden Pixel / Reference")]
    public Texture2D ReferenceTexture;
    public Color GoldenMarkerColor = new Color(1f, 0.843f, 0f, 1f);
    [Range(0, 60)] public int GoldenTolerance = 20;
    [Range(1, 64)] public int SampleRadius = 15;

    [Header("Matching")]
    [Range(1f, 80f)] public float ColorDistanceThreshold = 28f;
    public TerrainColorPalette Palette = new();

    [Header("Region Filter")]
    [Min(1)] public int MinRegionPixels = 64;
    [Min(1)] public int BigRegionPixelThreshold = 400;
    public bool ConnectDiagonals;

    public bool HasPalette => Palette != null && Palette.HasSamples;

    public void ApplyPaletteSettingsToPalette()
    {
        if (Palette == null)
            Palette = new TerrainColorPalette();

        Palette.GoldenMarkerColor = (Color32)GoldenMarkerColor;
        Palette.GoldenMarkerTolerance = GoldenTolerance;
        Palette.SampleRadius = SampleRadius;
        Palette.ColorDistanceThreshold = ColorDistanceThreshold;
    }

    public TerrainColorDetection.DetectionSettings ToDetectionSettings()
    {
        return new TerrainColorDetection.DetectionSettings
        {
            FeatureType = FeatureType,
            MinRegionPixels = Mathf.Max(1, MinRegionPixels),
            BigRegionPixelThreshold = Mathf.Max(1, BigRegionPixelThreshold),
            ConnectDiagonals = ConnectDiagonals
        };
    }
}
