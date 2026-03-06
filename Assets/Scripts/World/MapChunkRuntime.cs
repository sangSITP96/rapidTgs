using UnityEngine;

public class MapChunkRuntime : MonoBehaviour
{
    public Vector2Int Coord {  get; private set; }
    public MapChunkData Data { get; private set; }
    public Renderer GroundRenderer { get; private set; }

    public void Init(Vector2Int coord, MapChunkData data, Renderer rendererRef)
    {
        Coord = coord;
        Data = data;
        GroundRenderer = rendererRef;

        if(GroundRenderer != null && data != null && data.Visual != null)
        {
            GroundRenderer.material.mainTexture = data.Visual;
        }
    }
}
