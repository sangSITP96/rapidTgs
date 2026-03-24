using UnityEngine;

public class WorldTerrainQuery : MonoBehaviour
{
    [SerializeField] private InfiniteMapStreamer _streamer;
    [SerializeField] private LayerMask _groundLayer;

    [SerializeField] private ChunkUVMap _uvMap;


    [SerializeField, Range(0f, 1f)]
    private float lakeThreshold = 0.5f;

    [SerializeField, Range(0f, 1f)]
    private float forestThreshold = 0.5f;

    [SerializeField] private bool _useSlicedTiles = true;

    private Vector2 GetSampleUV(MapChunkRuntime chunk, Vector2 localUV)
    {
        if (_useSlicedTiles)
            return localUV;

        if (_streamer == null) return localUV;

        if (_uvMap != null && _uvMap.TryGetUV(chunk.Coord, out Rect uv))
        {
            return uv.position + Vector2.Scale(localUV, uv.size);
        }

        float u = (chunk.Coord.x + localUV.x) / (float)_streamer.Columns;
        float v = (chunk.Coord.y + localUV.y) / (float)_streamer.Rows;
        return new Vector2(u, v);
    }

    public float GetHeight(Vector3 worldPos)
    {
        if (!TryHit(worldPos, out var hit, out var chunk))
        {
            return 0.5f;
        }

        Vector2 globalUV = GetSampleUV(chunk, hit.textureCoord);

        return SampleGray(
            chunk.Data != null ? chunk.Data.Height:null,
            globalUV,
            0.5f
        );
    }

    public bool IsLake(Vector3 worldPos)
    {
        if (!TryHit(worldPos, out var hit, out var chunk) || chunk.Data == null)
            return false;

        Vector2 globalUV = GetSampleUV(chunk, hit.textureCoord);

        float s = SampleGray(chunk.Data.SmallLake, globalUV, 0f);
        if (s > lakeThreshold) return true;

        float b = SampleGray(chunk.Data.BigLake, globalUV, 0f);
        return b > lakeThreshold;
    }

    public bool IsForest(Vector3 worldPos)
    {
        if(!TryHit(worldPos, out var hit, out var chunk) || chunk.Data == null)
            return false;

        Vector2 globalUV = GetSampleUV(chunk, hit.textureCoord);

        float f = SampleGray(chunk.Data.Forest, globalUV, 0f);
        return f > forestThreshold;
    }

    private bool TryHit(Vector3 worldPos, out RaycastHit hit, out MapChunkRuntime chunk)
    {
        var ray = new Ray(worldPos + Vector3.up * 2f, Vector3.down);

        if(Physics.Raycast(ray, out hit, 100f, _groundLayer))
        {
            chunk = hit.collider.GetComponentInParent<MapChunkRuntime>();
            return chunk != null;
        }

        chunk = null;
        return false;
    }

    private float SampleGray(Texture2D tex, Vector2 uv, float fallback)
    {
        if (tex == null) return fallback;

        int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * tex.width), 0, tex.width - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * tex.height), 0, tex.height - 1);

        Color c = tex.GetPixel(x, y);

        return (c.r + c.g + c.b) / 3f;
    }
}
