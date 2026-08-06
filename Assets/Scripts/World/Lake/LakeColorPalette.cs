using System;
using UnityEngine;

[Serializable]
public struct LakeColorSample
{
    public Color32 Color;
    public int Count;

    public LakeColorSample(Color32 color, int count)
    {
        Color = color;
        Count = count;
    }
}

/// <summary>
/// Lake-specific palette type kept for existing bake assets / tooling compatibility.
/// Prefer <see cref="TerrainColorPalette"/> for new code.
/// </summary>
[Serializable]
public class LakeColorPalette
{
    public Color32 GoldenMarkerColor = new Color32(255, 215, 0, 255);

    public int GoldenMarkerTolerance = 20;
    public int SampleRadius = 15;

    public float ColorDistanceThreshold = 28f;
    public LakeColorSample[] Samples = Array.Empty<LakeColorSample>();

    public bool HasSamples => Samples != null && Samples.Length > 0;

    public bool Matches(Color32 pixel)
    {
        if (!HasSamples)
            return false;

        float best = float.MaxValue;

        for (int i = 0; i < Samples.Length; i++)
        {
            float distance = ColorDistance(Samples[i].Color, pixel);

            if (distance < best)
                best = distance;
        }

        return best <= ColorDistanceThreshold;
    }

    public static float ColorDistance(Color32 a, Color32 b)
    {
        return TerrainColorPalette.ColorDistance(a, b);
    }

    public static bool IsNearGolden(Color32 pixel, Color32 golden, int tolerance)
    {
        return TerrainColorPalette.IsNearGolden(pixel, golden, tolerance);
    }
}
