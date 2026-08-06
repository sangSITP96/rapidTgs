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
        {
            data.Visual.filterMode = FilterMode.Bilinear;
            data.Visual.wrapMode = TextureWrapMode.Clamp;
            data.Visual.anisoLevel = 1;
            mat.mainTexture = data.Visual;
        }

        mat.mainTextureScale = Vector2.one;
        mat.mainTextureOffset = Vector2.zero;

        RemoveLegacyLakeColliders();
    }

    public bool IsLakeAtUV(Vector2 localUV)
    {
        return Data != null && Data.IsLakeUV(localUV);
    }

    public bool IsLakeAtWorldPosition(Vector3 worldPosition)
    {
        return TryWorldToLocalUV(worldPosition, out Vector2 localUV) &&
               IsLakeAtUV(localUV);
    }

    public bool IsMountainAtUV(Vector2 localUV)
    {
        return Data != null && Data.IsMountainUV(localUV);
    }

    public bool IsMountainAtWorldPosition(Vector3 worldPosition)
    {
        return TryWorldToLocalUV(worldPosition, out Vector2 localUV) &&
               IsMountainAtUV(localUV);
    }

    public bool IsForestAtUV(Vector2 localUV)
    {
        return Data != null && Data.IsForestUV(localUV);
    }

    public bool IsForestAtWorldPosition(Vector3 worldPosition)
    {
        return TryWorldToLocalUV(worldPosition, out Vector2 localUV) &&
               IsForestAtUV(localUV);
    }

    public bool TryWorldToLocalUV(Vector3 worldPosition, out Vector2 localUV)
    {
        localUV = default;

        if (GroundRenderer == null)
            return false;

        Bounds bounds = GroundRenderer.localBounds;
        Vector3 size = bounds.size;

        if (Mathf.Approximately(size.x, 0f) || Mathf.Approximately(size.y, 0f))
            return false;

        Vector3 local = transform.InverseTransformPoint(worldPosition);
        localUV = new Vector2(
            Mathf.InverseLerp(bounds.min.x, bounds.max.x, local.x),
            Mathf.InverseLerp(bounds.min.y, bounds.max.y, local.y));

        return localUV.x >= 0f &&
               localUV.x <= 1f &&
               localUV.y >= 0f &&
               localUV.y <= 1f;
    }

    private void RemoveLegacyLakeColliders()
    {
        Transform staleColliders = transform.Find("LakeColliders");

        if (staleColliders == null)
            return;

        if (Application.isPlaying)
            Destroy(staleColliders.gameObject);
        else
            DestroyImmediate(staleColliders.gameObject);
    }
}
