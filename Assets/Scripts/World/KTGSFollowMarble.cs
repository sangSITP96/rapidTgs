using TGS;
using UnityEngine;

public class KTGSFollowMarble : MonoBehaviour
{
    [SerializeField] private TerrainGridSystem _tgs;
    [SerializeField] private Transform _marble;
    [SerializeField] private TileSingleHighlight _tileHighlight;

    [Header("Recenter")]
    [SerializeField] private float recenterDistance = 1.5f;

    [SerializeField] private bool snapToCell = true;
    [SerializeField] private bool clearCellSurfacesOnMove = true;

    private Vector3 _lastCenter;

    private void Start()
    {
        _lastCenter = _tgs.gridCenterWorldPosition;
    }

    private void LateUpdate()
    {
        Vector3 target = new Vector3(
            _marble.position.x,
            _lastCenter.y,
            _marble.position.z
        );

        float dist = Vector2.Distance(
            new Vector2(target.x, target.z),
            new Vector2(_lastCenter.x, _lastCenter.z)
        );

        if (dist < recenterDistance) return;

        if(_tileHighlight != null)
        {
            _tileHighlight.ClearCurrentHighlight();
        }    

        _tgs.HideHighlightedRegions();

        if(clearCellSurfacesOnMove)
        {
            _tgs.CellHideRegionSurfaces();
        }

        _tgs.SetGridCenterWorldPosition(target, snapToCell);

        _lastCenter = _tgs.gridCenterWorldPosition;
    }
}
