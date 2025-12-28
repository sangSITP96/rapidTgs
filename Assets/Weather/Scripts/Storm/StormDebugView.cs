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
        private Material _lineMaterial;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.useWorldSpace = false;
            _lineRenderer.loop = true;
            _lineRenderer.widthMultiplier = 0.02f;
    
            _lineMaterial = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.material = _lineMaterial;
    
            _lineRenderer.startColor = Color.red;
            _lineRenderer.endColor = Color.red;
    
            _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _lineRenderer.receiveShadows = false;
            // ========================================
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

        public void UpdateState(StormState state)
        {
            if (state == StormState.Forming)
            {
                SetStateForming();
            }
            else if (state == StormState.Active)
            {
                SetStateActive();
            }
        }
        
        public void SetStateForming()
        {
            // Smaller, pulsing circle
            _lineRenderer.startColor = Color.yellow;
            _lineRenderer.endColor = Color.yellow;
        }
        
        public void SetStateActive()
        {
            _lineRenderer.startColor = Color.red;
            _lineRenderer.endColor = Color.red;
        }
        
        private void OnDestroy()
        {
            // Destroy material khi object bị destroy
            if (_lineMaterial != null)
            {
                Destroy(_lineMaterial);
                _lineMaterial = null;
            }
        }
    }
}