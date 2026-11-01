using System;
using System.Collections.Generic;
using Game.General.UI;
using Game.Troops;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Campaign.UI
{
    public sealed class CampaignTroopSelectionPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _generalAdviceText;
        [SerializeField] private TMP_Text _selectedTotalText;
        [SerializeField] private Transform _listRoot;
        [SerializeField] private CampaignTroopSelectionRowView _rowPrefab;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _marchButton;

        private readonly List<CampaignTroopSelectionRowView> _rows = new List<CampaignTroopSelectionRowView>();
        private bool _scrollEnsured;

        public event Action BackClicked;

        /// <summary>
        /// Fired when Commander confirms selection and wants to review the march route.
        /// Does not start March by itself — CampaignFlowController handles route advice first.
        /// </summary>
        public event Action MarchClicked;

        private bool _fontsScaled;

        private void Awake()
        {
            GeneralPanelTextScale.ApplyOnce(this, ref _fontsScaled);
            if (_backButton != null)
                _backButton.onClick.AddListener(() => BackClicked?.Invoke());
            if (_marchButton != null)
                _marchButton.onClick.AddListener(() => MarchClicked?.Invoke());

            EnsureScrollableList();
            DisableDecorativeRaycasts();
        }

        public void Show(IReadOnlyList<TroopUnit> availableUnits, string generalAdvice = null)
        {
            GeneralPanelTextScale.ApplyOnce(this, ref _fontsScaled);
            EnsureScrollableList();
            DisableDecorativeRaycasts();

            gameObject.SetActive(true);
            if (_root != null && _root != gameObject)
                _root.SetActive(true);

            if (_titleText != null)
                _titleText.text = "MERCENARY TERMINAL";

            if (_generalAdviceText != null)
            {
                _generalAdviceText.text = string.Empty;
                _generalAdviceText.gameObject.SetActive(false);
            }

            if (_marchButton != null)
            {
                var label = _marchButton.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = "Review Route";
            }

            ClearRows();
            if (availableUnits != null && _rowPrefab != null && _listRoot != null)
            {
                for (int i = 0; i < availableUnits.Count; i++)
                {
                    CampaignTroopSelectionRowView row = Instantiate(_rowPrefab, _listRoot);
                    row.gameObject.SetActive(true);
                    ScaleRowFonts(row);
                    row.Bind(availableUnits[i]);
                    row.SelectionChanged += RefreshSelectedTotal;
                    _rows.Add(row);
                }
            }

            Canvas.ForceUpdateCanvases();
            RefreshSelectedTotal();
        }

        private static void ScaleRowFonts(CampaignTroopSelectionRowView row)
        {
            if (row == null)
                return;

            // Name uses TMP auto-size — do not bump its fontSize (that fights auto-fit).
            // Capacity / composition can take the panel scale bump.
            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null)
                    continue;

                text.raycastTarget = false;
                if (text.enableAutoSizing)
                {
                    text.fontSizeMax += GeneralPanelTextScale.ExtraSize;
                    continue;
                }

                text.fontSize += GeneralPanelTextScale.ExtraSize;
            }
        }

        public void Hide()
        {
            ClearRows();
            if (_root != null && _root != gameObject)
                _root.SetActive(false);
            gameObject.SetActive(false);
        }

        public List<TroopUnit> GetSelectedUnits()
        {
            var selected = new List<TroopUnit>();
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] != null && _rows[i].IsSelected && _rows[i].Unit != null)
                    selected.Add(_rows[i].Unit);
            }

            return selected;
        }

        private void RefreshSelectedTotal()
        {
            int total = 0;
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] != null && _rows[i].IsSelected && _rows[i].Unit != null)
                    total += _rows[i].Unit.GetAggregateTotal();
            }

            if (_selectedTotalText != null)
                _selectedTotalText.text = "Selected: " + total;

            if (_marchButton != null)
                _marchButton.interactable = total > 0;
        }

        private void ClearRows()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] != null)
                    Destroy(_rows[i].gameObject);
            }

            _rows.Clear();
        }

        private void DisableDecorativeRaycasts()
        {
            if (_titleText != null)
                _titleText.raycastTarget = false;
            if (_selectedTotalText != null)
                _selectedTotalText.raycastTarget = false;
            if (_generalAdviceText != null)
                _generalAdviceText.raycastTarget = false;
        }

        /// <summary>
        /// Phase 19 hire grows the army beyond the fixed list viewport. Without a ScrollRect,
        /// lower rows (often newly hired units) overflow under SelectedTotal / buttons and
        /// appear visible but cannot be clicked.
        /// </summary>
        private void EnsureScrollableList()
        {
            if (_scrollEnsured || _listRoot == null)
                return;

            _scrollEnsured = true;

            if (_listRoot.GetComponentInParent<ScrollRect>() != null)
                return;

            RectTransform oldContent = _listRoot as RectTransform;
            if (oldContent == null)
                return;

            Transform parent = oldContent.parent;
            if (parent == null)
                return;

            int siblingIndex = oldContent.GetSiblingIndex();
            Vector2 anchorMin = oldContent.anchorMin;
            Vector2 anchorMax = oldContent.anchorMax;
            Vector2 offsetMin = oldContent.offsetMin;
            Vector2 offsetMax = oldContent.offsetMax;

            var scrollGo = new GameObject("TroopListScroll", typeof(RectTransform));
            scrollGo.transform.SetParent(parent, false);
            scrollGo.transform.SetSiblingIndex(siblingIndex);

            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = anchorMin;
            scrollRt.anchorMax = anchorMax;
            scrollRt.offsetMin = offsetMin;
            scrollRt.offsetMax = offsetMax;

            var scrollBg = scrollGo.AddComponent<Image>();
            scrollBg.color = new Color(0f, 0f, 0f, 0.001f);
            scrollBg.raycastTarget = true;

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;

            var viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = Color.white;
            viewportImage.raycastTarget = true;

            var mask = viewportGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            oldContent.SetParent(viewportGo.transform, false);
            oldContent.name = "Content";
            oldContent.anchorMin = new Vector2(0f, 1f);
            oldContent.anchorMax = new Vector2(1f, 1f);
            oldContent.pivot = new Vector2(0.5f, 1f);
            oldContent.anchoredPosition = Vector2.zero;
            oldContent.offsetMin = new Vector2(0f, 0f);
            oldContent.offsetMax = new Vector2(0f, 0f);
            oldContent.sizeDelta = new Vector2(0f, 0f);

            var vlg = oldContent.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
                vlg = oldContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            var fitter = oldContent.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = oldContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRt;
            scroll.content = oldContent;
            _listRoot = oldContent;
        }
    }
}
