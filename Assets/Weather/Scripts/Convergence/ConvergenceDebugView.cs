using UnityEngine;

namespace Game.Weather.Convergence
{
    public class ConvergenceDebugView : MonoBehaviour
    {
        [SerializeField] private int _segments = 32;
        [SerializeField] private float _yOffset = 0.1f;
        [SerializeField] private float _defaultRadius = 1.5f;
        
        private LineRenderer _lineRenderer;
        private float _radius;
        private Material _lineMaterial;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.useWorldSpace = false;
            _lineRenderer.loop = true;
            _lineRenderer.widthMultiplier = 0.02f;
            
            _lineMaterial = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.material = _lineMaterial;
    
            _lineRenderer.startColor = Color.cyan;
            _lineRenderer.endColor = Color.cyan;
            
            _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _lineRenderer.receiveShadows = false;
            
            _radius = _defaultRadius;
            DrawCircle();
        }
        
        public void Initialize(float radius)
        {
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
        
        private void OnDestroy()
        {
            // Destroy material when object is destroyed
            if (_lineMaterial != null)
            {
                Destroy(_lineMaterial);
                _lineMaterial = null;
            }
        }
    }
}

