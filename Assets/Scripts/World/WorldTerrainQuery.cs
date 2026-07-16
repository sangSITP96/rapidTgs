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

    private Vector2 GetSampleUV(Vector2Int chunkCoord, Vector2 localUV)
    {
        if (_useSlicedTiles)
            return localUV;

        if (_streamer == null) return localUV;

        if (_uvMap != null && _uvMap.TryGetUV(chunkCoord, out Rect uv))
        {
            return uv.position + Vector2.Scale(localUV, uv.size);
        }

        float u = (chunkCoord.x + localUV.x) / (float)_streamer.Columns;
        float v = (chunkCoord.y + localUV.y) / (float)_streamer.Rows;
        return new Vector2(u, v);
    }

    private Vector2 GetSampleUV(MapChunkRuntime chunk, Vector2 localUV)
    {
        return GetSampleUV(chunk.Coord, localUV);
    }

    private bool IsLakeAtCoord(Vector2Int chunkCoord, MapChunkData data, Vector2 localUV)
    {
        if (data == null) return false;

        Vector2 sampleUV = GetSampleUV(chunkCoord, localUV);
        return IsLakeFromData(data, sampleUV);
    }

    public bool TryGetRandomLandPosition(
        Vector2Int chunkCoord,
        float edgePadding,
        int maxAttempts,
        out Vector3 worldPos)
    {
        worldPos = default;

        if (_streamer == null ||
            !_streamer.TryGetChunkWorldBounds(chunkCoord, out float minX, out float maxX, out float minZ, out float maxZ))
        {
            return false;
        }

        minX += edgePadding;
        maxX -= edgePadding;
        minZ += edgePadding;
        maxZ -= edgePadding;

        if (minX >= maxX || minZ >= maxZ)
            return false;

        MapChunkData data = _streamer.GetChunkData(chunkCoord);
        float y = _streamer.MarbleSpawnY;
        int attempts = Mathf.Max(1, maxAttempts);

        for (int i = 0; i < attempts; i++)
        {
            float x = Random.Range(minX, maxX);
            float z = Random.Range(minZ, maxZ);
            var localUV = new Vector2(
                Mathf.InverseLerp(minX, maxX, x),
                Mathf.InverseLerp(minZ, maxZ, z));

            if (!IsLakeAtCoord(chunkCoord, data, localUV))
            {
                worldPos = new Vector3(x, y, z);
                return true;
            }
        }

        float centerX = (minX + maxX) * 0.5f;
        float centerZ = (minZ + maxZ) * 0.5f;
        var centerUV = new Vector2(0.5f, 0.5f);

        if (!IsLakeAtCoord(chunkCoord, data, centerUV))
        {
            worldPos = new Vector3(centerX, y, centerZ);
            return true;
        }

        return false;
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
        if (TryHit(worldPos, out var hit, out var chunk) && chunk.Data != null)
        {
            Vector2 sampleUV = GetSampleUV(chunk, hit.textureCoord);
            return IsLakeFromData(chunk.Data, sampleUV);
        }

        if (_streamer != null &&
            _streamer.TryWorldToChunkLocalUV(worldPos, out var coord, out var localUV))
        {
            return IsLakeAtCoord(coord, _streamer.GetChunkData(coord), localUV);
        }

        return false;
    }

    public bool IsLakeAtChunk(Vector2Int chunkCoord, Vector2 localUV)
    {
        if (_streamer == null)
            return false;

        return IsLakeAtCoord(chunkCoord, _streamer.GetChunkData(chunkCoord), localUV);
    }

    private bool IsLakeFromData(MapChunkData data, Vector2 sampleUV)
    {
        if (data == null) return false;

        if (data.HasBakedLakes)
            return data.BakedLakes.IsBlockedUV(sampleUV);

        float s = SampleGray(data.SmallLake, sampleUV, 0f);
        if (s > lakeThreshold) return true;

        float b = SampleGray(data.BigLake, sampleUV, 0f);
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
