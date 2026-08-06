using System;
using UnityEngine;

[Serializable]
public struct TerrainColorSample
{
    public Color32 Color;
    public int Count;

    public TerrainColorSample(Color32 color, int count)
    {
        Color = color;
        Count = count;
    }
}

[Serializable]
public class TerrainColorPalette
{
    public Color32 GoldenMarkerColor = new Color32(255, 215, 0, 255);

    public int GoldenMarkerTolerance = 20;
    public int SampleRadius = 15;

    public float ColorDistanceThreshold = 28f;
    public TerrainColorSample[] Samples = Array.Empty<TerrainColorSample>();

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
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;

        return Mathf.Sqrt(dr * dr + dg * dg + db * db);
    }

    public static bool IsNearGolden(Color32 pixel, Color32 golden, int tolerance)
    {
        return Mathf.Abs(pixel.r - golden.r) <= tolerance &&
               Mathf.Abs(pixel.g - golden.g) <= tolerance &&
               Mathf.Abs(pixel.b - golden.b) <= tolerance;
    }

    public LakeColorPalette ToLakePalette()
    {
        return new LakeColorPalette
        {
            GoldenMarkerColor = GoldenMarkerColor,
            GoldenMarkerTolerance = GoldenMarkerTolerance,
            SampleRadius = SampleRadius,
            ColorDistanceThreshold = ColorDistanceThreshold,
            Samples = Samples != null && Samples.Length > 0
                ? Array.ConvertAll(Samples, s => new LakeColorSample(s.Color, s.Count))
                : Array.Empty<LakeColorSample>()
        };
    }

    public static TerrainColorPalette FromLakePalette(LakeColorPalette lake)
    {
        if (lake == null)
            return null;

        return new TerrainColorPalette
        {
            GoldenMarkerColor = lake.GoldenMarkerColor,
            GoldenMarkerTolerance = lake.GoldenMarkerTolerance,
            SampleRadius = lake.SampleRadius,
            ColorDistanceThreshold = lake.ColorDistanceThreshold,
            Samples = lake.Samples != null && lake.Samples.Length > 0
                ? Array.ConvertAll(lake.Samples, s => new TerrainColorSample(s.Color, s.Count))
                : Array.Empty<TerrainColorSample>()
        };
    }
}
