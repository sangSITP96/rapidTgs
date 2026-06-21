using JetBrains.Annotations;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class TerrainWorldBoundsProvider : MonoBehaviour
{
    [SerializeField] private Terrain _terrain;
    [SerializeField] private float _safetyPadding = 0.05f;

    private void Awake()
    {
        if(_terrain == null)
        {
            _terrain = Terrain.activeTerrain;
        }
    }

    public bool TryGetBounds(out Vector2 minXZ, out Vector2 maxXZ)
    {
        if(_terrain == null || _terrain.terrainData == null)
        {
            minXZ = Vector2.zero;
            maxXZ = Vector2.zero;
            return false;
        }

        Vector3 p = _terrain.GetPosition();
        Vector3 s = _terrain.terrainData.size;

        minXZ = new Vector2(p.x, p.z);
        maxXZ = new Vector2(p.x + s.x, p.z + s.z);

        return true;
    }

    public Vector3 ClampWorldXZ(Vector3 worldPos, float extraPadding = 0f)
    {
        if(!TryGetBounds(out var min, out var max))
            return worldPos;

        float pad = _safetyPadding + extraPadding;

        float x = Mathf.Clamp(worldPos.x, min.x + pad, max.x - pad);
        float z = Mathf.Clamp(worldPos.z, min.y + pad, max.y - pad);

        return new Vector3(x, worldPos.y, z);
    }

    public bool IsInsideXZ(Vector2 p, float margin = 0f)
    {
        if (!TryGetBounds(out var min, out var max))
            return true;

        return p.x >= min.x - margin && p.x <= max.x + margin &&
               p.y >= min.y - margin && p.y <= max.y + margin;
    }

    public Vector2 RandomInside(float padding = 0f)
    {
        TryGetBounds(out var min, out var max);

        float pad = _safetyPadding + padding;

        return new Vector2(
            Random.Range(min.x + pad, max.x - pad),
            Random.Range(min.y + pad, max.y - pad));
    }

    public Vector2 RandomOuterRing(float ringWidth = 4f)
    {
        TryGetBounds(out var min, out var max);

        float left = min.x - ringWidth;
        float right = max.x + ringWidth;
        float bottom = min.y - ringWidth;
        float top = max.y + ringWidth;
        
        int side = Random.Range(0, 4);

        switch(side)
        {
            case 0: // left
                return new Vector2(Random.Range(left, min.x), Random.Range(bottom, top));

            case 1: // right
                return new Vector2(Random.Range(max.x, right), Random.Range(bottom, top));
            
            case 2: // bottom
                return new Vector2(Random.Range(min.x, max.x), Random.Range(bottom, min.y));
            
            default: // top
                return new Vector2(Random.Range(min.x, max.x), Random.Range(max.y, top));
        }
    }
}
