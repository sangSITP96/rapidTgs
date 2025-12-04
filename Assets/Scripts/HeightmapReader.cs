using UnityEngine;

public class HeightmapReader : MonoBehaviour
{
    [SerializeField] private Renderer groundRenderer;
    [SerializeField] private Texture2D heightmap;

    public float GetHeightValueAtPosition(Vector3 worldPosition)
    {
        Vector3 localPosition =  groundRenderer.transform.InverseTransformPoint(worldPosition);
        Vector3 size = groundRenderer.transform.localScale;

        float u = (localPosition.x / size.x) +0.5f;
        float v = (localPosition.z / size.z) +0.5f;

        u = Mathf.Clamp01(u);
        v = Mathf.Clamp01(v);

        Color pixel = heightmap.GetPixelBilinear(u, v);
        return pixel.r;
    }
}
