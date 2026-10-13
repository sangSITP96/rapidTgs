using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Campaign.UI
{
    /// <summary>
    /// Separate debug/test controls — not part of main Campaign UX.
    /// </summary>
    public sealed class CampaignTestControlsView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _addReinforcementButton;
        [SerializeField] private Button _forceBattleButton;
        [SerializeField] private Button _advance2hButton;
        [SerializeField] private Button _advance6hButton;
        [SerializeField] private Button _completeNowButton;
        [SerializeField] private TMP_Text _hintText;

        public event Action AddReinforcementClicked;
        public event Action ForceBattleClicked;
        public event Action Advance2hClicked;
        public event Action Advance6hClicked;
        public event Action CompleteNowClicked;

        private void Awake()
        {
            Wire(_addReinforcementButton, () => AddReinforcementClicked?.Invoke());
            Wire(_forceBattleButton, () => ForceBattleClicked?.Invoke());
            Wire(_advance2hButton, () => Advance2hClicked?.Invoke());
            Wire(_advance6hButton, () => Advance6hClicked?.Invoke());
            Wire(_completeNowButton, () => CompleteNowClicked?.Invoke());

            if (_hintText != null)
                _hintText.text = "Campaign Test Controls (not main UX)";
        }

        public void SetVisible(bool visible)
        {
            if (_root != null)
                _root.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }

        private static void Wire(Button button, Action action)
        {
            if (button == null)
                return;
            button.onClick.AddListener(() => action?.Invoke());
        }
    }
}
