using UnityEngine;

namespace  Game.Utilities
{
    public class TerrainMaskUtility : MonoBehaviour
    {
        [Header("Terrain References")] [SerializeField]
        private Renderer _groundRenderer;
        [SerializeField] private LayerMask _groundLayerMask;

        [Header("Lake Mask")] [SerializeField] private Texture2D _smallLakeMaskTexture;

        [SerializeField] private Texture2D _bigLakeMaskTexture;

        [SerializeField, Range(0, 1f)] private float _lakeMaskThreshold = 0.5f;

        [Header("Forest Masks")] [SerializeField]
        private Texture2D _forestMaskTexture;
        [SerializeField, Range(0f, 1f)] private float _forestMaskThreshold = 0.5f;
        
        private static TerrainMaskUtility _instance;

        public static TerrainMaskUtility Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<TerrainMaskUtility>();
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Debug.LogWarning("Multiple TerrainMaskUtility instances found. Using the first one.");
            }
        }

        public bool IsLake(Vector3 worldPos)
        {
            if (_smallLakeMaskTexture != null)
            {
                float value = GetTextureValueAtPosition(_smallLakeMaskTexture, worldPos);
                bool isLake = value > _lakeMaskThreshold;

                if (!isLake && _bigLakeMaskTexture != null)
                {
                    value = GetTextureValueAtPosition(_bigLakeMaskTexture, worldPos);
                    return value > _lakeMaskThreshold;
                }

                return isLake;
            }
            return false;
        }

        public bool IsInForest(Vector3 worldPos)
        {
            if (_forestMaskTexture == null)
            {
                return false;
            }

            float forestValue = GetTextureValueAtPosition(_forestMaskTexture, worldPos);
            return forestValue > _forestMaskThreshold;
        }

        public float GetTextureValueAtPosition(Texture2D texture, Vector3 worldPosition)
        {
            if (texture == null)
            {
                return 0f;
            }
            
            Vector2 uv = GetUVFromWorldPosition(worldPosition);
            if (uv == Vector2.zero)
            {
                return 0f;
            }
            
            int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * texture.width), 0, texture.width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * texture.height), 0, texture.height - 1);
            Color pixel = texture.GetPixel(x, y);

            return (pixel.r + pixel.g + pixel.b) / 3f;
        }

        private Vector2 GetUVFromWorldPosition(Vector3 worldPosition)
        {
            if (_groundRenderer == null)
            {
                return Vector2.zero;
            }
            
            Ray ray = new Ray(worldPosition + Vector3.up * 1f, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 50f, _groundLayerMask))
            {
                return hit.textureCoord;
            }
            
            return Vector2.zero;
        }
        
        public Vector2 GetUVFromWorldPositionFast(Vector3 worldPosition)
        {
            if (_groundRenderer == null)
            {
                return Vector2.zero;
            }

            Vector3 localPos = _groundRenderer.transform.InverseTransformPoint(worldPosition);
            Vector3 size = _groundRenderer.transform.localScale;

            float u = (localPos.x / size.x) + 0.5f;
            float v = (localPos.z / size.z) + 0.5f;

            return new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v));
        }
    }
}

