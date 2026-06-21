using System;
using UnityEngine;
using UnityEngine.UI;

public class ColonyEventLogRowView : MonoBehaviour
{
    [SerializeField] private Text _tileText;
    [SerializeField] private Text _categoryTimeText;

    [SerializeField] private Button _detailButton;
    [SerializeField] private GameObject _selectedGameObject;

    private Action<ColonyEventEntry> _onDetail;

    private ColonyEventEntry _eventData;

    private bool _isSelect = false;

    private void Awake()
    {
        _detailButton.onClick.RemoveAllListeners();
        _detailButton.onClick.AddListener(() => _onDetail?.Invoke(_eventData));
    }

    public void CallClickDetail()
    {
        _onDetail?.Invoke(_eventData);
    }

    public void Bind(ColonyEventEntry entry)
    {
        if(entry == null)
        {
            return;
        }
        _eventData = entry;
        if (_tileText != null)
        {
            _tileText.text = entry.Title;
        }

        if(_categoryTimeText != null)
        {
            DateTimeOffset time = DateTimeOffset
                .FromUnixTimeMilliseconds(entry.TimestampUnixMsUtc)
                .ToLocalTime();

            _categoryTimeText.text = $"{time:yyyy-MM-dd HH:mm} UTC";
        }    
    }    

    public void OnClickDetail(Action<ColonyEventEntry> onClickDetail)
    {
        _onDetail = onClickDetail;
    }

    public void Select(bool isSelect)
    {
        _isSelect = isSelect;
        _selectedGameObject.SetActive(isSelect);
    }

    public string GetEventId => _eventData.Id;

}
