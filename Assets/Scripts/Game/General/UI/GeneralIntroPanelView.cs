using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.General.UI
{
    /// <summary>
    /// Full-screen intro panel shown from Develop Mode → General.
    /// Artwork + speech bubble (General talking) + Open Navigation.
    /// </summary>
    public sealed class GeneralIntroPanelView : MonoBehaviour
    {
        private const string DefaultGeneralName = "THE GENERAL";

        private const string DefaultDialogue =
            "Now that you've done a practice run, let me show you on the War Table.\n\n" +
            "This is a real operation. Start your first campaign now. It's a 60 second march.\n\n" +
            "Tap on the navigation system to see the route, then send your mercenaries to the destination and begin your campaign.";

        [SerializeField] private GameObject _root;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private GameObject _speechBubbleRoot;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private Button _openNavigationButton;
        [SerializeField] private Button _closeButton;

        [Header("Dialogue")]
        [SerializeField] private string _generalName = DefaultGeneralName;
        [TextArea(4, 10)]
        [SerializeField] private string _dialogueText = DefaultDialogue;

        public event Action OpenNavigationClicked;
        public event Action CloseClicked;

        private void Awake()
        {
            WireButtons();
            ApplyDialogue();
        }

        /// <summary>
        /// Assign refs after runtime construction (Awake may have already run).
        /// </summary>
        public void Configure(
            GameObject root,
            Image backgroundImage,
            Button openNavigationButton,
            Button closeButton,
            GameObject speechBubbleRoot = null,
            TMP_Text nameText = null,
            TMP_Text messageText = null)
        {
            _root = root;
            _backgroundImage = backgroundImage;
            _openNavigationButton = openNavigationButton;
            _closeButton = closeButton;
            _speechBubbleRoot = speechBubbleRoot;
            _nameText = nameText;
            _messageText = messageText;
            WireButtons();
            ApplyDialogue();
        }

        public void Show(Sprite portrait = null)
        {
            EnsureSpeechBubble();

            gameObject.SetActive(true);
            if (_root != null && _root != gameObject)
                _root.SetActive(true);

            if (_speechBubbleRoot != null)
                _speechBubbleRoot.SetActive(true);

            if (_backgroundImage != null && portrait != null)
                _backgroundImage.sprite = portrait;

            ApplyDialogue();
            transform.SetAsLastSibling();
        }

        /// <summary>
        /// Adds a speech bubble at runtime if the scene panel was built before this UI existed.
        /// </summary>
        public void EnsureSpeechBubble()
        {
            if (_speechBubbleRoot != null && _nameText != null && _messageText != null)
                return;

            Transform parent = _root != null ? _root.transform : transform;
            Transform existing = parent.Find("SpeechBubble");
            if (existing != null)
            {
                _speechBubbleRoot = existing.gameObject;
                if (_nameText == null)
                    _nameText = existing.Find("Name")?.GetComponent<TMP_Text>();
                if (_messageText == null)
                    _messageText = existing.Find("Message")?.GetComponent<TMP_Text>();
                if (_nameText != null && _messageText != null)
                    return;
            }

            _speechBubbleRoot = GeneralIntroSpeechBubbleFactory.Create(
                parent,
                out _nameText,
                out _messageText);
        }

        public void Hide()
        {
            if (_root != null && _root != gameObject)
                _root.SetActive(false);
            gameObject.SetActive(false);
        }

        public void SetPortrait(Sprite portrait)
        {
            if (_backgroundImage != null && portrait != null)
                _backgroundImage.sprite = portrait;
        }

        public void SetDialogue(string generalName, string message)
        {
            if (!string.IsNullOrEmpty(generalName))
                _generalName = generalName;
            if (message != null)
                _dialogueText = message;
            ApplyDialogue();
        }

        private void ApplyDialogue()
        {
            if (_nameText != null)
                _nameText.text = string.IsNullOrEmpty(_generalName) ? DefaultGeneralName : _generalName;

            if (_messageText != null)
                _messageText.text = string.IsNullOrEmpty(_dialogueText) ? DefaultDialogue : _dialogueText;
        }

        private void WireButtons()
        {
            if (_openNavigationButton != null)
            {
                _openNavigationButton.onClick.RemoveListener(OnOpenNavigation);
                _openNavigationButton.onClick.AddListener(OnOpenNavigation);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnClose);
                _closeButton.onClick.AddListener(OnClose);
            }
        }

        private void OnOpenNavigation() => OpenNavigationClicked?.Invoke();

        private void OnClose() => CloseClicked?.Invoke();
    }
}
