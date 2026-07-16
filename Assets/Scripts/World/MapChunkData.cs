using UnityEngine;

[CreateAssetMenu(menuName = "World/Map Chunk Data")]
public class MapChunkData : ScriptableObject
{
    public Texture2D Visual;
    public Texture2D Height;
    public Texture2D SmallLake;
    public Texture2D BigLake;
    public Texture2D Forest;

    [Header("Baked Lake / Collider")]
    public BakedLakeChunkData BakedLakes = new();

    public bool HasBakedLakes =>
        BakedLakes != null &&
        BakedLakes.IsLocked &&
        BakedLakes.HasMask;

    public bool IsLakeUV(Vector2 uv)
    {
        if(HasBakedLakes)
            return BakedLakes.IsBlockedUV(uv);

       return false;
    }
}
