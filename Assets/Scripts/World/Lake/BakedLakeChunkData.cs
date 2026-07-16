using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BakedLakeRegion
{
    public int Id;
    public int PixelCount;
    public RectInt PixelBounds;
    public Vector2 CenterUV;
    public bool IsBig;

    public BakedLakeRegion()
    { 
    }

    public BakedLakeRegion(
        int id,
        int pixelCount,
        RectInt pixelBounds,
        Vector2 centerUV,
        bool isBig)
    {
        Id = id;
        PixelCount = pixelCount;
        PixelBounds = pixelBounds;
        CenterUV = centerUV;
        IsBig = isBig;
    }
}

[Serializable]
public class BakedLakeChunkData
{
    public bool IsLocked;
    public int TextureWidth;
    public int TextureHeight;
    public long BakedUtcTicks;


    public byte[] BlockedMask;
    public List<BakedLakeRegion> Regions = new();

    public bool HasMask =>
        BlockedMask != null &&
        TextureWidth > 0 &&
        TextureHeight > 0 &&
        BlockedMask.Length == TextureWidth * TextureHeight;

    public bool IsBlockedPixel(int x, int y)
    {
        if(!HasMask)
            return false;

        if(x < 0 || y < 0 || x >= TextureWidth || y >= TextureHeight)
            return false;

        return BlockedMask[y * TextureWidth + x] != 0;
    }

    public bool IsBlockedUV(Vector2 uv)
    {
        if(!HasMask)
            return false;

        int x = Mathf.Clamp(
            Mathf.FloorToInt(uv.x * TextureWidth),
            0,
            TextureWidth - 1);

        int y = Mathf.Clamp(
           Mathf.FloorToInt(uv.y * TextureHeight),
           0,
           TextureHeight - 1);

        return BlockedMask[y * TextureWidth + x] != 0;
    }

    public void Clear()
    {
        IsLocked = false;
        TextureWidth = 0;
        TextureHeight = 0;
        BakedUtcTicks = 0;
        BlockedMask = null;
        Regions = new List<BakedLakeRegion>();
    }
}
