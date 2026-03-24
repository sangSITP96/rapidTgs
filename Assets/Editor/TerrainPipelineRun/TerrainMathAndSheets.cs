using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public struct BaseVariation
{
    public float heightDetailAmp;
    public float heghtDetailFreq;
    public float forestMaskDetailAmp;
    public float lakeMaskDetailAmp;
    public float visualDetailAmp;
}

public struct TransitionVariation
{
    public float noiseFreq;
    public float edgeWidth;
    public float edgeDetailAmp;
}

public static class TerrainMathAndSheets
{
    private static float Gray(Color32 c)
    {
        return (c.r + c.g + c.b) / (3f * 255f);
    }

    public static float Smoothstep(float a, float b, float t)
    {
        t = Mathf.Clamp01((t - a) / (b - a + 1e-6f));
        return t * t * (3f - 2f * t);
    }

    private static float Fbm01(float x, float y, int seed, int octaves)
    {
        float sum = 0f;
        float amp = 1f;
        float freq = 1;

        float sx = seed * 0.001f;
        float sy = seed * 0.002f;

        for (int i = 0; i < octaves; i++)
        {
            float n = Mathf.PerlinNoise(x * freq + sx, y * freq + sy);
            sum += n * amp;

            amp *= 0.5f;
            freq *= 2;
        }

        return Mathf.Clamp01(sum / 1.75f);
    }

    // Base Sheet
    public static GeneratedSheet BuildBaseSheet(
        BiomeInputs src,
        int res,
        int seed,
        BaseVariation v
    )
    {
        Color32[] hPix = src.Height.GetPixels32();
        Color32[] vPix = src.Visual.GetPixels32();
        Color32[] fPix = src.ForestMask.GetPixels32();
        Color32[] lPix = src.LakeMask.GetPixels32();

        int count = res * res;

        float[] outH = new float[count];
        float[] outF = new float[count];
        float[] outL = new float[count];
        Color32[] outV = new Color32[count];

        float minH = float.MaxValue;
        float maxH = float.MinValue;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                int i = y * res + x;

                float u = (res <= 1) ? 0f : x / (float)(res - 1);
                float w = (res <= 1) ? 0f : y / (float)(res - 1);

                float n1 = Fbm01(u * v.heghtDetailFreq, w * v.heghtDetailFreq, seed + 111, 3);
                float n2 = Fbm01(
                    (u + n1 * 0.15f) * (v.heghtDetailFreq * 0.6f),
                    (w - n1 * 0.15f) * (v.heghtDetailFreq * 0.6f),
                    seed + 222,
                    2
                );

                float baseH = Gray(hPix[i]);
                float h = baseH + v.heightDetailAmp * (n1 * 0.6f + n2 * 0.4f);

                outH[i] = h;
                minH = Mathf.Min(minH, h);
                maxH = Mathf.Max(maxH, h);

                float baseF = Gray(fPix[i]);
                float baseL = Gray(lPix[i]);

                outF[i] = Mathf.Clamp01(baseF + v.forestMaskDetailAmp * (n2 - 0.5f));
                outL[i] = Mathf.Clamp01(baseL + v.lakeMaskDetailAmp * (n1 - 0.5f));

                Color32 bc = vPix[i];

                float r = bc.r / 255f;
                float g = bc.g / 255f;
                float b = bc.b / 255f;

                outV[i] = new Color32(
                    (byte)(r * 255f),
                    (byte)(g * 255f),
                    (byte)(b * 255f),
                    255
                );
            }


        }
        //Normalize height
        float range = maxH - minH;
        if (range < 1e-6f) range = 1f;

        Texture2D heightOut = new Texture2D(res, res, TextureFormat.RGBA32, false, false);
        Texture2D forestOut = new Texture2D(res, res, TextureFormat.RGBA32, false, false);
        Texture2D lakeOut = new Texture2D(res, res, TextureFormat.RGBA32, false, false);
        Texture2D visualtOut = new Texture2D(res, res, TextureFormat.RGBA32, false, false);

        Color32[] heightPixels = new Color32[count];
        Color32[] forestPixels = new Color32[count];
        Color32[] lakePixels = new Color32[count];

        for (int i = 0; i < count; i++)
        {
            float hn = Mathf.Clamp01((outH[i] - minH) / range);
            float fn = Mathf.Clamp01(outF[i]);
            float ln = Mathf.Clamp01(outL[i]);

            byte hb = (byte)(hn * 255f);
            byte fb = (byte)(fn * 255f);
            byte lb = (byte)(ln * 255f);

            heightPixels[i] = new Color32(hb, hb, hb, 255);
            forestPixels[i] = new Color32(fb, fb, fb, 255);
            lakePixels[i] = new Color32(lb, lb, lb, 255);
        }

        heightOut.SetPixels32(heightPixels);
        forestOut.SetPixels32(forestPixels);
        lakeOut.SetPixels32(lakePixels);
        visualtOut.SetPixels32(outV);

        heightOut.Apply();
        forestOut.Apply();
        lakeOut.Apply();
        visualtOut.Apply();

        return new GeneratedSheet
        {
            Resolution = res,
            Height = heightOut,
            Visual = visualtOut,
            ForestMask = forestOut,
            LakeMask = lakeOut
        };

    }

    public static GeneratedSheet BuildTransitionSheet(
        GeneratedSheet a,
        GeneratedSheet b,
        TerrainBiome biomeA,
        TerrainBiome biomeB,
        int res,
        int seed,
        TransitionVariation t)
    {
        Color32[] hA = a.Height.GetPixels32();
        Color32[] hB = b.Height.GetPixels32();

        Color32[] vA = a.Visual.GetPixels32();
        Color32[] vB = b.Visual.GetPixels32();

        Color32[] fA = a.ForestMask.GetPixels32();
        Color32[] fB = b.ForestMask.GetPixels32();

        Color32[] lA = a.LakeMask.GetPixels32();
        Color32[] lB = b.LakeMask.GetPixels32();

        int count = res * res;

        float[] outH = new float[count];
        float[] outF = new float[count];
        float[] outL = new float[count];
        Color32[] outV = new Color32[count];

        float minH = float.MaxValue;
        float maxH = float.MinValue;

        bool involvesLake = biomeA == TerrainBiome.Lake || biomeB == TerrainBiome.Lake;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                int i = y * res + x;

                float u = (res <= 1) ? 0f : x / (float)(res - 1);
                float v = (res <= 1) ? 0f : y / (float)(res - 1);

                //Domain warp
                float warpU = Fbm01(u * 2.1f, v * 2.1f, seed + 777, 2);
                float warpV = Fbm01((u + 10.5f) * 1.7f, (v - 3.2f) * 1.7f, seed + 8882, 2);

                float uu = u + warpU * 0.08f;
                float vv = v + warpV * 0.08f;

                float n = Fbm01(uu * t.noiseFreq, vv * t.noiseFreq, seed + 999, 3);

                float M = Smoothstep(
                    0.5f - t.edgeWidth,
                    0.5f + t.edgeWidth,
                    n
                );

                float edgeDetail = t.edgeDetailAmp * (Fbm01(uu * t.noiseFreq * 1.9f, vv * t.noiseFreq * 1.9f, seed + 123, 2) - 0.5f);

                M = Mathf.Clamp01(M + edgeDetail);

                float ha = Gray(hA[i]);
                float hb = Gray(hB[i]);
                float h = Mathf.Lerp(ha, hb, M);

                float fa = Gray(fA[i]);
                float fb = Gray(fB[i]);
                float forestMask = Mathf.Lerp(fa, fb, M);

                float la = Gray(lA[i]);
                float lb = Gray(lB[i]);
                float lakeMask = Mathf.Lerp(la, lb, M);

                if(involvesLake)
                {
                    float boundaryWet = 1f - Mathf.Abs(M - 0.5f) * 2f;
                    float boost = 0.35f * boundaryWet;
                    float maxL = Mathf.Max(la, lb);

                    lakeMask = Mathf.Clamp01(lakeMask + boost * maxL);
                }
                else
                {
                    lakeMask = Mathf.Clamp01(lakeMask * 0.75f);
                }

                Color32 ca = vA[i];
                Color32 cb = vB[i];

                float r = Mathf.Lerp(ca.r / 255f, cb.r / 255f, M);
                float g = Mathf.Lerp(ca.g / 255f, cb.g / 255f, M);
                float bC = Mathf.Lerp(ca.b / 255f, cb.b / 255f, M);

                outH[i] = h;
                outF[i] = forestMask;
                outL[i] = lakeMask;

                minH = Mathf.Min(minH, h);
                maxH = Mathf.Max(maxH, h);

                outV[i] = new Color32(
                    (byte)(Mathf.Clamp01(r) * 255f),
                    (byte)(Mathf.Clamp01(g) * 255f),
                    (byte)(Mathf.Clamp01(bC) * 255f),
                    255
                );
            }
        }

        float range = maxH - minH;
        if (range < 1e-6f) range = 1f;

        Texture2D heightOut = new Texture2D(res, res, TextureFormat.RGBA32, false, false);
        Texture2D forestOut = new Texture2D(res, res, TextureFormat.RGBA32, false, false);
        Texture2D lakeOut = new Texture2D(res, res, TextureFormat.RGBA32, false, false);
        Texture2D visualOut = new Texture2D(res, res, TextureFormat.RGBA32, false, false);

        Color32[] heightPixels = new Color32[count];
        Color32[] forestPixels = new Color32[count];
        Color32[] lakePixels = new Color32[count];

        for(int i = 0; i<count;i++)
        {
            float hn = Mathf.Clamp01((outH[i] - minH) / range);
            float fn = Mathf.Clamp01(outF[i]);
            float ln = Mathf.Clamp01(outL[i]);

            byte hb = (byte)(hn * 255f);
            byte fb = (byte)(fn * 255f);
            byte lb = (byte)(ln * 255f);

            heightPixels[i] = new Color32(hb, hb, hb, 255);
            forestPixels[i] = new Color32(fb, fb, fb, 255);
            lakePixels[i] = new Color32(lb, lb, lb, 255);
        }

        heightOut.SetPixels32(heightPixels);
        forestOut.SetPixels32(forestPixels);
        lakeOut.SetPixels32(lakePixels);
        visualOut.SetPixels32(outV);

        heightOut.Apply();
        forestOut.Apply();
        lakeOut.Apply();
        visualOut.Apply();

        return new GeneratedSheet
        {
            Resolution = res,
            Height = heightOut,
            Visual = visualOut,
            ForestMask = forestOut,
            LakeMask = lakeOut
        };
    }
}
