using UnityEngine;

[CreateAssetMenu(menuName = "World/Map Chunk Data")]
public class MapChunkData : ScriptableObject
{
    public Texture2D Visual;
    public Texture2D Height;
    public Texture2D SmallLake;
    public Texture2D BigLake;
    public Texture2D Forest;

    [Header("Baked Terrain Features")]
    public BakedLakeChunkData BakedLakes = new();
    public BakedLakeChunkData BakedMountains = new();
    public BakedLakeChunkData BakedForests = new();

    public bool HasBakedLakes => IsBaked(BakedLakes);
    public bool HasBakedMountains => IsBaked(BakedMountains);
    public bool HasBakedForests => IsBaked(BakedForests);

    public bool IsLakeUV(Vector2 uv)
    {
        return HasBakedLakes && BakedLakes.IsBlockedUV(uv);
    }

    public bool IsLakePixel(int x, int y)
    {
        return HasBakedLakes && BakedLakes.IsBlockedPixel(x, y);
    }

    public bool IsMountainUV(Vector2 uv)
    {
        return HasBakedMountains && BakedMountains.IsBlockedUV(uv);
    }

    public bool IsMountainPixel(int x, int y)
    {
        return HasBakedMountains && BakedMountains.IsBlockedPixel(x, y);
    }

    public bool IsForestUV(Vector2 uv)
    {
        return HasBakedForests && BakedForests.IsBlockedUV(uv);
    }

    public bool IsForestPixel(int x, int y)
    {
        return HasBakedForests && BakedForests.IsBlockedPixel(x, y);
    }

    public BakedLakeChunkData GetBakedData(TerrainFeatureType type)
    {
        switch (type)
        {
            case TerrainFeatureType.Mountain:
                return BakedMountains;
            case TerrainFeatureType.Forest:
                return BakedForests;
            default:
                return BakedLakes;
        }
    }

    public void SetBakedData(TerrainFeatureType type, BakedLakeChunkData data)
    {
        switch (type)
        {
            case TerrainFeatureType.Mountain:
                BakedMountains = data ?? new BakedLakeChunkData();
                break;
            case TerrainFeatureType.Forest:
                BakedForests = data ?? new BakedLakeChunkData();
                break;
            default:
                BakedLakes = data ?? new BakedLakeChunkData();
                break;
        }
    }

    private static bool IsBaked(BakedLakeChunkData data)
    {
        return data != null && data.IsLocked && data.HasMask;
    }
}
