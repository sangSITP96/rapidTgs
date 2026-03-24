using UnityEngine;

public class MapChunkRuntime : MonoBehaviour
{
    public Vector2Int Coord {  get; private set; }
    public MapChunkData Data { get; private set; }
    public Renderer GroundRenderer { get; private set; }

    public void Init(Vector2Int coord, MapChunkData data, Renderer rendererRef, int gridColumns = 1, int gridRows = 1, ChunkUVMap uvMap = null)
    {
        Coord = coord;
        Data = data;
        GroundRenderer = rendererRef;

        if (GroundRenderer == null || data == null) return;

        var mat = GroundRenderer.material;
        if (data.Visual != null)
            mat.mainTexture = data.Visual;

        mat.mainTextureScale = Vector2.one;
        mat.mainTextureOffset = Vector2.zero;

        Debug.Log(
            $"[MapChunkRuntime] Coord={coord} | Using sliced texture '{data.Visual?.name}'",
            this
        );
    }
}
