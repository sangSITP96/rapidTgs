using UnityEngine;
using UnityEngine.UI;

namespace Game.Morale.UI
{
    public sealed class MoraleHUD : MonoBehaviour
    {
        [Header("System")]
        [SerializeField]
        private TroopMoraleSystem moraleSystem;

        [SerializeField] private MoraleSettings settings;

        [SerializeField] private GameObject torchRoot;
        
        [SerializeField]
        private Button torchButton;

        [SerializeField] private GameObject panelRoot;

        [SerializeField] private Button closeButton;
        
        [Header("Meters")]
        [SerializeField]
        private MoraleMeterView moraleMeter;
        
        [SerializeField]
        private MoraleMeterView healthMeter;
        
        [SerializeField]
        private MoraleMeterView sleepMeter;
        
        [SerializeField]
        private MoraleMeterView waterMeter;
        
        [SerializeField]
        private MoraleMeterView foodMeter;

        private void Awake()
        {
            if(torchButton != null)
                torchButton.onClick.AddListener(Open);
            
            if(closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        private void OnEnable()
        {
            if (moraleSystem != null)
                moraleSystem.StateChanged += Refresh;

            Refresh();
        }

        private void OnDisable()
        {
            if (moraleSystem != null)
                moraleSystem.StateChanged -= Refresh;
        }

        private void OnDestroy()
        {
            if(torchButton != null)
                torchButton.onClick.RemoveListener(Open);
            
            if(closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }

        public void Open()
        {
            if(torchRoot != null)
                torchRoot.SetActive(false);

            if (panelRoot != null)
                panelRoot.SetActive(true);

            Refresh();
        }

        public void Close()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
            
            if(torchRoot != null)
                torchRoot.SetActive(true);
        }

        public void Refresh()
        {
            if(moraleSystem == null || settings == null)
                return;
            
            SetMeter(moraleMeter, moraleSystem.Morale);
            SetMeter(healthMeter, moraleSystem.Health);
            SetMeter(sleepMeter, moraleSystem.Sleep);
            SetMeter(waterMeter, moraleSystem.Water);
            SetMeter(foodMeter, moraleSystem.Food);
        }

        private void SetMeter(
            MoraleMeterView meter,
            float value)
        {
            if(meter == null)
                return;
            
            meter.SetValue(value, settings.GetMeterColor(value));
        }
    }
}

