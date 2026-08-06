using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct BakedLakeRowSpan
{
    public int Y;
    public int XMin;
    public int XMax;

    public BakedLakeRowSpan(int y, int xMin, int xMax)
    {
        Y = y;
        XMin = xMin;
        XMax = xMax;
    }

    public bool Contains(int x, int y)
    {
        return y == Y && x >= XMin && x <= XMax;
    }
}

[Serializable]
public class BakedLakeRegion
{
    public int Id;
    public int PixelCount;
    public RectInt PixelBounds;
    public Vector2 CenterUV;
    public bool IsBig;

    [HideInInspector]
    public List<BakedLakeRowSpan> RowSpans = new();

    /// <summary>
    /// Ordered outer contour in UV space (continuous walk for shoreline navigation).
    /// Closed loop: last point connects back to first.
    /// </summary>
    [HideInInspector]
    public List<Vector2> PerimeterOrderedUVs = new();

    public BakedLakeRegion()
    {
    }

    public BakedLakeRegion(
        int id,
        int pixelCount,
        RectInt pixelBounds,
        Vector2 centerUV,
        bool isBig,
        List<BakedLakeRowSpan> rowSpans,
        List<Vector2> perimeterOrderedUVs = null)
    {
        Id = id;
        PixelCount = pixelCount;
        PixelBounds = pixelBounds;
        CenterUV = centerUV;
        IsBig = isBig;
        RowSpans = rowSpans ?? new List<BakedLakeRowSpan>();
        PerimeterOrderedUVs = perimeterOrderedUVs ?? new List<Vector2>();
    }

    public bool ContainsPixel(int x, int y)
    {
        if (!PixelBounds.Contains(new Vector2Int(x, y)) || RowSpans == null)
            return false;

        for (int i = 0; i < RowSpans.Count; i++)
        {
            if (RowSpans[i].Contains(x, y))
                return true;
        }

        return false;
    }
}

/// <summary>
/// Baked occupancy for one terrain feature type on a chunk.
/// Class name kept for Lake asset serialization compatibility; also used for Forest/Mountain.
/// </summary>
[Serializable]
public class BakedLakeChunkData
{
    public bool IsLocked;
    public TerrainFeatureType FeatureType = TerrainFeatureType.Lake;
    public int TextureWidth;
    public int TextureHeight;
    public long BakedUtcTicks;

    public List<BakedLakeRegion> Regions = new();

    public bool HasMask =>
        TextureWidth > 0 &&
        TextureHeight > 0 &&
        Regions != null &&
        HasAnyRowSpan();

    public bool IsBlockedPixel(int x, int y)
    {
        if (!HasMask)
            return false;

        if (x < 0 || y < 0 || x >= TextureWidth || y >= TextureHeight)
            return false;

        for (int i = 0; i < Regions.Count; i++)
        {
            BakedLakeRegion region = Regions[i];
            if (region != null && region.ContainsPixel(x, y))
                return true;
        }

        return false;
    }

    public bool IsBlockedUV(Vector2 uv)
    {
        if (!HasMask)
            return false;

        int x = Mathf.Clamp(
            Mathf.FloorToInt(uv.x * TextureWidth),
            0,
            TextureWidth - 1);

        int y = Mathf.Clamp(
            Mathf.FloorToInt(uv.y * TextureHeight),
            0,
            TextureHeight - 1);

        return IsBlockedPixel(x, y);
    }

    public void Clear()
    {
        IsLocked = false;
        TextureWidth = 0;
        TextureHeight = 0;
        BakedUtcTicks = 0;
        Regions = new List<BakedLakeRegion>();
    }

    private bool HasAnyRowSpan()
    {
        for (int i = 0; i < Regions.Count; i++)
        {
            BakedLakeRegion region = Regions[i];
            if (region != null && region.RowSpans != null && region.RowSpans.Count > 0)
                return true;
        }

        return false;
    }
}
