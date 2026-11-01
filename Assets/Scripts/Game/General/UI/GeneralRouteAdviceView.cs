using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.General.UI
{
    public sealed class GeneralRouteAdviceView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Image _portraitImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private TMP_Text _bodyText;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _approveMarchButton;

        public event Action BackClicked;
        public event Action ApproveMarchClicked;

        private bool _fontsScaled;

        private void Awake()
        {
            GeneralPanelTextScale.ApplyOnce(this, ref _fontsScaled);
            if (_backButton != null)
                _backButton.onClick.AddListener(() => BackClicked?.Invoke());
            if (_approveMarchButton != null)
                _approveMarchButton.onClick.AddListener(() => ApproveMarchClicked?.Invoke());
        }

        public void Show(string generalName, GeneralRouteAdvice advice, Sprite portrait = null)
        {
            GeneralPanelTextScale.ApplyOnce(this, ref _fontsScaled);
            gameObject.SetActive(true);
            if (_root != null && _root != gameObject)
                _root.SetActive(true);

            if (_portraitImage != null && portrait != null)
                _portraitImage.sprite = portrait;

            if (_nameText != null)
                _nameText.text = string.IsNullOrEmpty(generalName) ? "ROUTE APPROVAL" : generalName;

            if (_messageText != null)
            {
                _messageText.text = string.Empty;
                _messageText.gameObject.SetActive(false);
            }

            if (_bodyText != null)
                _bodyText.text = FormatBody(advice);

            bool canApprove = advice != null && advice.IsValid;
            if (_approveMarchButton != null)
                _approveMarchButton.interactable = canApprove;
        }

        public void Hide()
        {
            if (_root != null && _root != gameObject)
                _root.SetActive(false);
            gameObject.SetActive(false);
        }

        private static string FormatBody(GeneralRouteAdvice advice)
        {
            if (advice == null)
                return string.Empty;

            var sb = new StringBuilder();
            if (advice.BodyLines != null)
            {
                for (int i = 0; i < advice.BodyLines.Count; i++)
                {
                    string line = advice.BodyLines[i];
                    if (IsTechnicalLine(line))
                        continue;
                    if (sb.Length > 0)
                        sb.Append('\n');
                    sb.Append(line);
                }
            }

            if (advice.RiskFactors != null && advice.RiskFactors.Count > 0)
            {
                bool wroteHeader = false;
                for (int i = 0; i < advice.RiskFactors.Count; i++)
                {
                    AdviceFactor factor = advice.RiskFactors[i];
                    if (factor == null || factor.Kind == AdviceFactorKind.Unknown)
                        continue;
                    string text = factor.ToString();
                    if (IsTechnicalLine(text))
                        continue;
                    if (!wroteHeader)
                    {
                        if (sb.Length > 0)
                            sb.Append("\n\nRisk factors:");
                        wroteHeader = true;
                    }

                    sb.Append("\n• ").Append(text);
                }
            }

            return sb.ToString();
        }

        private static bool IsTechnicalLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return true;
            return line.IndexOf("MissingDependencies", System.StringComparison.OrdinalIgnoreCase) >= 0
                   || line.IndexOf("TgsBiomeMapData missing", System.StringComparison.OrdinalIgnoreCase) >= 0
                   || line.IndexOf("Route invalid", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
