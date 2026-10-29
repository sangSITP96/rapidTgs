using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.General.UI
{
    public sealed class GeneralBriefingView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Image _portraitImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private TMP_Text _bodyText;
        [SerializeField] private Button _declineButton;
        [SerializeField] private Button _acceptButton;

        public event Action AcceptClicked;
        public event Action DeclineClicked;

        private bool _fontsScaled;

        private void Awake()
        {
            GeneralPanelTextScale.ApplyOnce(this, ref _fontsScaled);
            if (_declineButton != null)
                _declineButton.onClick.AddListener(() => DeclineClicked?.Invoke());
            if (_acceptButton != null)
                _acceptButton.onClick.AddListener(() => AcceptClicked?.Invoke());
        }

        public void Show(string generalName, GeneralBriefingAdvice advice, Sprite portrait = null)
        {
            GeneralPanelTextScale.ApplyOnce(this, ref _fontsScaled);
            gameObject.SetActive(true);
            if (_root != null && _root != gameObject)
                _root.SetActive(true);

            if (_portraitImage != null && portrait != null)
                _portraitImage.sprite = portrait;

            if (_nameText != null)
                _nameText.text = string.IsNullOrEmpty(generalName) ? "CAMPAIGN SELECTION" : generalName;

            if (_messageText != null)
            {
                string message = advice != null ? advice.GeneralMessage : string.Empty;
                _messageText.text = message ?? string.Empty;
                _messageText.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
            }

            if (_bodyText != null)
                _bodyText.text = FormatBody(advice);
        }

        public void Hide()
        {
            if (_root != null && _root != gameObject)
                _root.SetActive(false);
            gameObject.SetActive(false);
        }

        private static string FormatBody(GeneralBriefingAdvice advice)
        {
            if (advice == null)
                return string.Empty;

            if (advice.BodyLines != null && advice.BodyLines.Count > 0)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < advice.BodyLines.Count; i++)
                {
                    if (i > 0)
                        sb.Append('\n');
                    sb.Append(advice.BodyLines[i]);
                }

                return sb.ToString();
            }

            return string.Empty;
        }
    }
}
