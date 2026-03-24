using NUnit.Framework.Constraints;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class TerrainPipelineTextureIO
{
    private static float Gray(Color32 c)
        => (c.r + c.g + c.b) / (3f * 255);

    private static string AssetPathToFull(string assetPath)
    {
        string dataPath = Application.dataPath.Replace("\\", "/");
        string ap = assetPath.Replace("\\", "/");

        if (!ap.StartsWith("Assets/"))
            return null;

        string rel = ap.Substring("Assets/".Length);
        return Path.Combine(dataPath, rel);
    }

    private static string FullToAssetPath(string fullPath)
    {
        string dataPath = Application.dataPath.Replace("\\", "/");
        string fp = fullPath.Replace("\\", "/");

        if(!fp.StartsWith(dataPath))
        {
            return null;
        }

        string rel = fp.Substring(dataPath.Length).TrimStart('/');
        return "Assets/" + rel;
    }

    private static void EnsureDir(string fullDir)
    {
        if(!Directory.Exists(fullDir))
        {
            Directory.CreateDirectory(fullDir);
        }
    }

    public static void SaveCroppedColor(
        Texture2D src,
        int ox,
        int oy,
        int size,
        string fullPngPath,
        bool sRGB,
        bool readable)
    {
        if(!src.isReadable)
        {
            throw new System.InvalidOperationException($"Texture '{src.name}' is not readable.");
        }

        Color[] srcColors = src.GetPixels(ox, oy, size, size);
        Color32[] pix = new Color32[srcColors.Length];
        for (int i = 0; i < srcColors.Length; i++)
            pix[i] = srcColors[i];

        Texture2D tmp = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
        tmp.SetPixels32(pix);
        tmp.Apply();

        SaveTextureAsPngWithImporter(tmp, fullPngPath, sRGB, readable);
        UnityEngine.Object.DestroyImmediate(tmp);
    }

    public static void SaveCroppedGray(
        Texture2D src,
        int ox,
        int oy,
        int size,
        string fullPngPath,
        bool sRGB,
        bool readable)
    {
        if(!src.isReadable)
        {
            throw new System.InvalidOperationException($"Texture '{src.name}' is not readable.");
        }

        Color[] srcColors = src.GetPixels(ox, oy, size, size);
        Color32[] pix = new Color32[srcColors.Length];
        for (int i = 0; i < srcColors.Length; i++)
            pix[i] = srcColors[i];

        Color32[] outPix = new Color32[pix.Length];

        for(int i=0;i<pix.Length;i++)
        {
            byte b = (byte)(Mathf.Clamp01(Gray(pix[i])) * 255f);
            outPix[i] = new Color32(b, b, b, 255);
        }

        Texture2D tmp = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
        tmp.SetPixels32(outPix);
        tmp.Apply();

        SaveTextureAsPngWithImporter(tmp, fullPngPath, sRGB, readable);
        UnityEngine.Object.DestroyImmediate(tmp);
    }

    // Derive small/big lake masks

    public static void SaveDeriveSmallAndBigLakes(
        Texture2D lakeMaskSrc,
        int ox,
        int oy,
        int size,
        float unityLakeThreshold,
        float bigLakethreshold,
        float smoothWidth,
        string fullSmallPngPath,
        string fullBigPngPath,
        bool sRGB,
        bool readable)
    {
        if(!lakeMaskSrc.isReadable)
        {
            throw new InvalidOperationException($"Texture '{lakeMaskSrc.name}' is not readable.");
        }

        Color32[] pix = ReadCropAsColor32(lakeMaskSrc, ox, oy, size);

        Color32[] smallPix = new Color32[pix.Length];
        Color32[] bigPix = new Color32[pix.Length];

        for(int i =0; i< pix.Length; i++)
        {
            float v = Gray(pix[i]);

            float smallStep = TerrainMathAndSheets.Smoothstep(
                unityLakeThreshold - smoothWidth,
                unityLakeThreshold + smoothWidth,
                v);

            float bigStep = TerrainMathAndSheets.Smoothstep(
                bigLakethreshold - smoothWidth,
                bigLakethreshold + smoothWidth,
                v);

            float small = smallStep * (1f - bigStep);
            float big = bigStep;

            byte sb = (byte)(Mathf.Clamp01(small) * 255f);
            byte bb = (byte)(Mathf.Clamp01(big) * 255f);

            smallPix[i] = new Color32(sb, sb, sb, 255);
            bigPix[i] = new Color32(bb, bb, bb, 255);
        }

        Texture2D smallTmp = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
        smallTmp.SetPixels32(smallPix);
        smallTmp.Apply();

        Texture2D bigTmp = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
        bigTmp.SetPixels32(bigPix);
        bigTmp.Apply();

        SaveTextureAsPngWithImporter(smallTmp, fullSmallPngPath, sRGB, readable);
        SaveTextureAsPngWithImporter(bigTmp, fullBigPngPath, sRGB, readable);

        UnityEngine.Object.DestroyImmediate(smallTmp);
        UnityEngine.Object.DestroyImmediate(bigTmp);
    }

    private static void SaveTextureAsPngWithImporter(
        Texture2D tex,
        string fullPngPath,
        bool sRGB,
        bool readable)
    {
        if (tex == null) return;

        string dir = Path.GetDirectoryName(fullPngPath);
        EnsureDir(dir);

        byte[] png = tex.EncodeToPNG();
        File.WriteAllBytes(fullPngPath, png);

        string assetPath = FullToAssetPath(fullPngPath);
        if(string.IsNullOrEmpty(assetPath))
        {
            throw new System.Exception($"Saved png is not under Assets/: {fullPngPath}");
        }

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if(importer != null)
        {
            importer.sRGBTexture = sRGB;
            importer.isReadable = readable;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            importer.SaveAndReimport();
        }

    }

    private static Color32[] ReadCropAsColor32(Texture2D src, int ox, int oy, int size)
    {
        // Texture2D has GetPixels(x,y,w,h), not GetPixels32(x,y,w,h)
        Color[] colors = src.GetPixels(ox, oy, size, size);
        Color32[] outPixels = new Color32[colors.Length];
        for (int i = 0; i < colors.Length; i++)
            outPixels[i] = colors[i];
        return outPixels;
    }
}
