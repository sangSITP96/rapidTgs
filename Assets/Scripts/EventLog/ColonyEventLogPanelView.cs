using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColonyEventLogPanelView : MonoBehaviour
{
    [SerializeField] private ColonyEventLogService _service;
    [SerializeField] private ScrollRect _scroll;
    [SerializeField] private ColonyEventLogRowView _rowPrefab;
    [SerializeField] private ColonyEventLogDetailView _detailView;

    private List<ColonyEventLogRowView> _listRowView = new List<ColonyEventLogRowView>();

    private void OnEnable()
    {
        if (_service == null)
        {
            _service = ColonyEventLogService.Instance;
        }

        if(_service != null)
        {
            _service.OnLogChanged += Rebuild;
        }

        Rebuild();
    }

    private void OnDisable()
    {
        if(_service != null)
        {
            _service.OnLogChanged -= Rebuild;
        }
    }

    private void Rebuild()
    {
        if(_service == null || _scroll == null || _rowPrefab == null)
        {
            return;
        }
        _listRowView = new List<ColonyEventLogRowView>();
        Transform content = _scroll.content;

        for(int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }

        var list = _service.Events;
        Debug.Log("List Count: " + list.Count);

        for (int i = list.Count - 1; i>=0;i--)
        {
            var row = Instantiate(_rowPrefab, content);
            row.Bind(list[i]);
            row.OnClickDetail(colonyData =>
            {
                _detailView.ShowColonyEventDetail(colonyData);
                OnSelectDetailEventLog(colonyData);
            });

            _listRowView.Add(row);
        }

        Canvas.ForceUpdateCanvases();

        _scroll.verticalNormalizedPosition = 1f;
        StartCoroutine(IECallClickDetail());
    }

    private IEnumerator IECallClickDetail()
    {
        yield return new WaitForSeconds(0.2f);
        if (_listRowView.Count > 0)
        {
            _listRowView[0].CallClickDetail();
        }
    }

    private void OnSelectDetailEventLog(ColonyEventEntry colonyData)
    {
        foreach(var rowView in _listRowView)
        {
            rowView.Select(rowView.GetEventId == colonyData.Id);
        }
    }
}
