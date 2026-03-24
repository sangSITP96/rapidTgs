using UnityEngine;

[CreateAssetMenu(menuName = "World/Chunk UV Map")]
public class ChunkUVMap : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public Vector2Int coord; // chunk (x,y)
        public Rect uv;          // uMin,vMin,width,height  (0..1)
    }

    public Entry[] entries;

    public bool TryGetUV(Vector2Int coord, out Rect uv)
    {
        foreach (var e in entries)
        {
            if (e.coord == coord)
            {
                uv = e.uv;
                return true;
            }
        }
        uv = default;
        return false;
    }
}
