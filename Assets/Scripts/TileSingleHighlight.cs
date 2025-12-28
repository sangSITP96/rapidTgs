using TGS;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class TileSingleHighlight : MonoBehaviour
{
   [SerializeField] private TerrainGridSystem _terrainGridSystem;
   [SerializeField]  private Camera _camera;
   
   [Header("Highlight Settings")]
   [SerializeField] private Color _highlightColor = new  Color(1, 1, 0.8f, 0.6f);
   [FormerlySerializedAs("_hightlightDuration")] [SerializeField] private float _highlightDuration = 0.5f;
   [SerializeField] private bool _fadeOut = true;

   private int _currentHighlightCell = -1;
   private float _highlightStartTime;

   void Awake()
   {
      if (_camera == null)
      {
         _camera = Camera.main;
      }

      if (_terrainGridSystem == null)
      {
         _terrainGridSystem = FindObjectOfType<TerrainGridSystem>();
      }
   }

   private void Start()
   {
      if (_terrainGridSystem != null)
      {
         _terrainGridSystem.highlightMode = HighlightMode.None;
      }
   }

   private void Update()
   {
      HandleTickClick();
      UpdateHighlightFade();
   }

   private void HandleTickClick()
   {
      if (!Input.GetMouseButtonDown(0))
         return;

      if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
         return;
      
      Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
      if(!Physics.Raycast(ray, out RaycastHit hit))
         return;

      var cellSelected = _terrainGridSystem.CellGetAtPosition(hit.point, true);
      var cellIndex = _terrainGridSystem.CellGetIndex(cellSelected);
      
      if (cellIndex < 0)
         return;

      HighlightCell(cellIndex);
   }

   private void HighlightCell(int cellIndex)
   {
      if (_currentHighlightCell >= 0)
      {
         ClearHighlight();
      }


      _terrainGridSystem.CellToggleRegionSurface(cellIndex, true, _highlightColor);
      _currentHighlightCell = cellIndex;
      _highlightStartTime = Time.time;
   }

   private void UpdateHighlightFade()
   {
      if (_currentHighlightCell < 0)
         return;
      float elapsed = Time.time - _highlightStartTime;

      if (elapsed >= _highlightDuration)
      {
         ClearHighlight();
      }
      else if (_fadeOut)
      {
         float fadeProgress = elapsed/_highlightDuration;
         Color fadeColor = _highlightColor;
         fadeColor.a = _highlightColor.a * (1f - fadeProgress);
         
         _terrainGridSystem.CellSetColor(_currentHighlightCell, fadeColor);
      }
   }

   private void ClearHighlight()
   {
      if (_currentHighlightCell >= 0)
      {
         _terrainGridSystem.CellHideRegionSurface(_currentHighlightCell);
         _currentHighlightCell = -1;
      }
   }

   public void ClearCurrentHighlight()
   {
      ClearHighlight();
   }
}
