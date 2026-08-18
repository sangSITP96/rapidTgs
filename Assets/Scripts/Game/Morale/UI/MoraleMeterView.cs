using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Morale.UI
{
    public sealed class MoraleMeterView : MonoBehaviour
    {
        [SerializeField] private Slider slider;

        [SerializeField] private TMP_Text percentageText;

        public void SetValue(
            float value,
            Color color)
        {
            value = Mathf.Clamp(value, 0f, 100f);

            if (slider != null)
            {
                slider.value = value/100f;
                var fillImage = slider.fillRect.GetComponent<Image>();
                if(fillImage != null)
                    fillImage.color = color;
            }

            if (percentageText != null)
            {
                percentageText.SetText("{0:0}%", value);
            }
        }
    }
}