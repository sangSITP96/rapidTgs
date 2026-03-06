using UnityEngine;

[CreateAssetMenu(menuName = "World/Map Chunk Data")]
public class MapChunkData : ScriptableObject
{
    public Texture2D Visual;
    public Texture2D Height;
    public Texture2D SmallLake;
    public Texture2D BigLake;
    public Texture2D Forest;
}
