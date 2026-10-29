using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.General.UI
{
    public sealed class GeneralMonitoringView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Image _portraitImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private Button _restButton;
        [SerializeField] private Button _resumeButton;

        public event Action RestClicked;
        public event Action ResumeClicked;

        private bool _fontsScaled;

        private void Awake()
        {
            GeneralPanelTextScale.ApplyOnce(this, ref _fontsScaled);
            if (_restButton != null)
                _restButton.onClick.AddListener(() => RestClicked?.Invoke());
            if (_resumeButton != null)
                _resumeButton.onClick.AddListener(() => ResumeClicked?.Invoke());
        }

        public void Show(string generalName, GeneralMonitoringAdvice advice, Sprite portrait = null)
        {
            GeneralPanelTextScale.ApplyOnce(this, ref _fontsScaled);
            gameObject.SetActive(true);
            if (_root != null && _root != gameObject)
                _root.SetActive(true);

            Refresh(generalName, advice, portrait);
        }

        public void Refresh(string generalName, GeneralMonitoringAdvice advice, Sprite portrait = null)
        {
            if (_portraitImage != null && portrait != null)
                _portraitImage.sprite = portrait;

            if (_nameText != null)
                _nameText.text = string.IsNullOrEmpty(generalName) ? "MARCH STATUS" : generalName;

            if (_messageText != null)
            {
                _messageText.text = string.Empty;
                _messageText.gameObject.SetActive(false);
            }

            if (_statusText != null && advice != null)
            {
                _statusText.text =
                    $"Travel: {advice.TravelStatus}\n" +
                    $"Morale {advice.Morale:0}  Health {advice.Health:0}\n" +
                    $"Sleep {advice.Sleep:0}  Food {advice.Food:0}  Water {advice.Water:0}" +
                    (advice.RecommendRest && !string.IsNullOrEmpty(advice.RestRecommendationReason)
                        ? "\n\n" + advice.RestRecommendationReason
                        : string.Empty);
            }

            if (_restButton != null)
            {
                _restButton.gameObject.SetActive(advice != null && advice.CanOfferRestAction);
                _restButton.interactable = advice != null && advice.CanOfferRestAction;
                var label = _restButton.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = advice != null && advice.RecommendRest ? "Rest (Recommended)" : "Rest";
            }

            if (_resumeButton != null)
            {
                _resumeButton.gameObject.SetActive(advice != null && advice.CanOfferResumeAction);
                _resumeButton.interactable = advice != null && advice.CanOfferResumeAction;
            }
        }

        public void Hide()
        {
            if (_root != null && _root != gameObject)
                _root.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}
