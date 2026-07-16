using UnityEngine;

public class MapChunkRuntime : MonoBehaviour
{
    public Vector2Int Coord {  get; private set; }
    public MapChunkData Data { get; private set; }
    public Renderer GroundRenderer { get; private set; }

    private Transform _lakeColliderRoot;

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
        ApplyBakedLakeColliders();
    }

    public void ApplyBakedLakeColliders()
    {
        if (Data == null || !Data.HasBakedLakes || Data.BakedLakes.Regions == null)
            return;

        int textureWidth = Data.BakedLakes.TextureWidth;
        int textureHeight = Data.BakedLakes.TextureHeight;

        if (textureWidth <= 0 || textureHeight <= 0)
            return;

        Bounds meshBounds = GroundRenderer != null
            ? GroundRenderer.localBounds
            : new Bounds(Vector3.zero, Vector3.one);

        Vector3 boundsMin = meshBounds.min;
        Vector3 boundsSize = meshBounds.size;

        var root = new GameObject("LakeColliders");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        _lakeColliderRoot = root.transform;

        foreach (BakedLakeRegion region in Data.BakedLakes.Regions)
        {
            if (region == null || region.PixelCount <= 0)
                continue;

            float uMin = region.PixelBounds.xMin / (float) textureWidth;
            float uMax = region.PixelBounds.xMax / (float) textureWidth;
            float vMin = region.PixelBounds.yMin / (float) textureHeight;
            float vMax = region.PixelBounds.yMax / (float)textureHeight;

            float centerU = (uMin + uMax) * 0.5f;
            float centerV = (vMin + vMax) * 0.5f;

            float sizeU = Mathf.Max(0.001f, uMax - uMin);
            float sizeV = Mathf.Max(0.001f, vMax - vMin);

            GameObject colliderObject = new GameObject($"LakeCollider_{region.Id}");
            colliderObject.transform.SetParent(_lakeColliderRoot, false);

            colliderObject.transform.localPosition = new Vector3(
                boundsMin.x + centerU * boundsSize.x,
                boundsMin.y + centerV * boundsSize.y,
                boundsMin.z + boundsSize.z * 0.5f + 0.02f);

            colliderObject.transform.localRotation = Quaternion.identity;
            colliderObject.transform.localScale = Vector3.one;

            BoxCollider box = colliderObject.AddComponent<BoxCollider>();
            box.isTrigger = false;
            box.center = Vector3.zero;

            box.size = new Vector3(
                sizeU * Mathf.Max(0.001f, boundsSize.x),
                sizeV * Mathf.Max(0.001f, boundsSize.y),
                Mathf.Max(0.05f, boundsSize.z + 0.1f));
        }
    }

    public void ClearLakeColliders()
    {
        if (_lakeColliderRoot != null)
        {
            if (Application.isPlaying)
                Destroy(_lakeColliderRoot.gameObject);
            else
                DestroyImmediate(_lakeColliderRoot.gameObject);

            _lakeColliderRoot = null;
            return;
        }

        Transform existing = transform.Find("LakeColliders");

        if (existing != null)
        {
            if (Application.isPlaying)
                Destroy(existing.gameObject);
            else
                DestroyImmediate(existing.gameObject);
        }
    }
}
