using UnityEngine;
using UnityEngine.Serialization;

public class CameraZoomController : MonoBehaviour
{
   [SerializeField] private Camera _cam;

   [Header("Zoom")] 
   [SerializeField] private float _zoomSpeed = 0.2f;
   [SerializeField] private float _pinchZoomSpeed = 0.02f;

   [FormerlySerializedAs("_minZoom")] [SerializeField] private float _minOrthographicSize = 0.7f;
   [FormerlySerializedAs("_maxZoom")] [SerializeField] private float _maxOrthographicSize = 1.3f;

   private void Awake()
   {
      if (_cam == null)
      {
         _cam = Camera.main;
      }
   }

   private void Update()
   {
      HandleMouseZoom();
      HandleTouchZoom();
   }

   private void HandleMouseZoom()   // PC
   {
      if (Input.mouseScrollDelta.y == 0)
         return;
      
      _cam.orthographicSize -= Input.mouseScrollDelta.y * _zoomSpeed;
      ClampZoom();
   }

   private void HandleTouchZoom() //Mobile, Ipad
   {
      if(Input.touchCount != 2)
         return;
      
      Touch t0 = Input.GetTouch(0);
      Touch t1 = Input.GetTouch(1);

      Vector2 prevPos0 = t0.position - t0.deltaPosition;
      Vector2 prevPos1 = t1.position - t1.deltaPosition;
      
      float prevDist = Vector2.Distance(prevPos0, prevPos1);
      float currDist = Vector2.Distance(t0.position, t1.position);
      
      float delta = currDist - prevDist;
      
      _cam.orthographicSize -= delta * _pinchZoomSpeed;
      ClampZoom();
   }

   private void ClampZoom()
   {
      _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize, _minOrthographicSize, _maxOrthographicSize);
   }
   
}
