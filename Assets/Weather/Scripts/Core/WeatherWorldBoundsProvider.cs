using UnityEngine;
using UnityEngine.Rendering;

public class WeatherWorldBoundsProvider : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private InfiniteMapStreamer _mapStreamer;

    [Header("Fallback (if streamer missing)")]
    [SerializeField] private Vector2 _fallbackMin = new Vector2(-4.375f, -3.125f);
    [SerializeField] private Vector2 _fallbackMax = new Vector2(4.375f, 3.125f);

    public bool TryGetBounds(out Vector2 min, out Vector2 max)
    {
        if(_mapStreamer != null &&
            _mapStreamer.TryGetWorldBounds(out float minX, out float maxX,
            out float minZ, out float maxZ))
        {
            min = new Vector2(minX, minZ);
            max = new Vector2(maxX, maxZ);
            return true;
        }

        min = _fallbackMin;
        max = _fallbackMax;
        return true;
    }

    public Vector2 Clamp(Vector2 p, float padding = 0f)
    {
        TryGetBounds(out var min, out var max);

        return new Vector2(
            Mathf.Clamp(p.x, min.x + padding, max.x - padding),
            Mathf.Clamp(p.y, min.y + padding, max.y - padding)
        );
    }

    public bool IsInside(Vector2 p, float margin = 0f)
    {
        TryGetBounds(out var min, out var max);

        return p.x >= min.x - margin && p.x <= max.x + margin &&
            p.y >= min.y - margin && p.y <= max.y + margin;
    }

    public Vector2 RandomInside()
    {
        TryGetBounds(out var min, out var max);

        return new Vector2(
            Random.Range(min.x, max.x),
            Random.Range(min.y, max.y)
        );
    }

    public Vector2 RandomOuterRing(float ringWidth = 2f)
    {
        TryGetBounds(out var min, out var max);

        float left = min.x - ringWidth;
        float right = max.x + ringWidth;
        float bottom = min.y - ringWidth;
        float top = max.y + ringWidth;

        int side = Random.Range(0, 4);

        switch (side)
        {
            case 0: return new Vector2(Random.Range(left, min.x), Random.Range(bottom, top));
            case 1: return new Vector2(Random.Range(max.x, right), Random.Range(bottom, top));
            case 2: return new Vector2(Random.Range(min.x, max.x), Random.Range(bottom, min.y));
            default: return new Vector2(Random.Range(min.x, max.x), Random.Range(max.y, top));
        }

    }

}
