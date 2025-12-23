using UnityEngine;

namespace Game.Weather.Storm
{
    [RequireComponent(typeof(LineRenderer))]
    public class StormDebugView : MonoBehaviour
    {
        [SerializeField] private int _segments = 32;
        [SerializeField] private float _yOffset = 0.1f;
        
        private LineRenderer _lineRenderer;
        private float _radius;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.useWorldSpace = false;
            _lineRenderer.loop = true;
            _lineRenderer.widthMultiplier = 0.02f;
    
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
    
            _lineRenderer.startColor = Color.red;
            _lineRenderer.endColor = Color.red;
    
            _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _lineRenderer.receiveShadows = false;
            // ========================================
        }

        public void Initialize(float radius)
        {
            UnityEngine.Debug.Log("Draw Circleeee");
            _radius = radius;
            DrawCircle();
        }

        private void DrawCircle()
        {
            _lineRenderer.positionCount = _segments;
            for (int i = 0; i < _segments; i++)
            {
                float angle = (float)i / _segments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * _radius;
                float z = Mathf.Sin(angle) * _radius;
                
                _lineRenderer.SetPosition(i, new Vector3(x, _yOffset, z));
            }
        }
    }
}