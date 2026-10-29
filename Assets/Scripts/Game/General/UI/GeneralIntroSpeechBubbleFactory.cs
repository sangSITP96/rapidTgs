using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.General.UI
{
    /// <summary>
    /// Builds the parchment speech-bubble overlay for GeneralIntroPanel.
    /// </summary>
    public static class GeneralIntroSpeechBubbleFactory
    {
        private static readonly Color Parchment = new Color(0.93f, 0.88f, 0.76f, 0.96f);
        private static readonly Color Border = new Color(0.22f, 0.16f, 0.1f, 1f);
        private static readonly Color Ink = new Color(0.18f, 0.12f, 0.08f, 1f);

        public static GameObject Create(
            Transform parent,
            out TMP_Text nameText,
            out TMP_Text messageText)
        {
            var bubble = new GameObject("SpeechBubble", typeof(RectTransform), typeof(Image));
            bubble.transform.SetParent(parent, false);
            var bubbleRt = bubble.GetComponent<RectTransform>();
            // Top-left, next to the General (as in the mockup).
            bubbleRt.anchorMin = new Vector2(0.03f, 0.52f);
            bubbleRt.anchorMax = new Vector2(0.46f, 0.94f);
            bubbleRt.offsetMin = Vector2.zero;
            bubbleRt.offsetMax = Vector2.zero;
            var bubbleBg = bubble.GetComponent<Image>();
            bubbleBg.color = Parchment;
            bubbleBg.raycastTarget = false;

            var outline = bubble.AddComponent<Outline>();
            outline.effectColor = Border;
            outline.effectDistance = new Vector2(2.5f, -2.5f);

            // Small tail pointing toward the General (right side of bubble).
            var tail = new GameObject("Tail", typeof(RectTransform), typeof(Image));
            tail.transform.SetParent(bubble.transform, false);
            var tailRt = tail.GetComponent<RectTransform>();
            tailRt.anchorMin = new Vector2(1f, 0.42f);
            tailRt.anchorMax = new Vector2(1f, 0.42f);
            tailRt.pivot = new Vector2(0f, 0.5f);
            tailRt.sizeDelta = new Vector2(22f, 18f);
            tailRt.anchoredPosition = new Vector2(-2f, 0f);
            tailRt.localRotation = Quaternion.Euler(0f, 0f, -28f);
            var tailImg = tail.GetComponent<Image>();
            tailImg.color = Parchment;
            tailImg.raycastTarget = false;

            nameText = CreateTmp(
                bubble.transform,
                "Name",
                "THE GENERAL",
                new Vector2(0.06f, 0.78f),
                new Vector2(0.94f, 0.96f),
                27f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);

            var divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            divider.transform.SetParent(bubble.transform, false);
            var divRt = divider.GetComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0.12f, 0.74f);
            divRt.anchorMax = new Vector2(0.88f, 0.755f);
            divRt.offsetMin = Vector2.zero;
            divRt.offsetMax = Vector2.zero;
            var divImg = divider.GetComponent<Image>();
            divImg.color = new Color(Border.r, Border.g, Border.b, 0.55f);
            divImg.raycastTarget = false;

            messageText = CreateTmp(
                bubble.transform,
                "Message",
                string.Empty,
                new Vector2(0.06f, 0.06f),
                new Vector2(0.94f, 0.72f),
                21f,
                FontStyles.Normal,
                TextAlignmentOptions.TopLeft);
            messageText.enableWordWrapping = true;
            messageText.overflowMode = TextOverflowModes.Overflow;

            return bubble;
        }

        private static TextMeshProUGUI CreateTmp(
            Transform parent,
            string name,
            string text,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float fontSize,
            FontStyles style,
            TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = Ink;
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
