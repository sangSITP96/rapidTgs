using UnityEngine;

namespace Game.Seasons
{
    /// <summary>
    /// Future seasonal foliage/terrain hooks. No artwork required for Phase 14 Alpha.
    /// </summary>
    public sealed class SeasonVisualHookRelay : MonoBehaviour
    {
        [SerializeField] private SeasonSystem _seasonSystem;

        public event System.Action<SeasonId, float> OnSeasonAppearanceChanged;

        private void OnEnable()
        {
            if (_seasonSystem == null)
                _seasonSystem = SeasonSystem.Instance ?? FindFirstObjectByType<SeasonSystem>();

            if (_seasonSystem != null)
            {
                _seasonSystem.OnSeasonChanged += HandleSeasonChanged;
                _seasonSystem.OnSeasonTransitionProgress += HandleTransition;
                _seasonSystem.OnSeasonVisualHook += HandleVisualHook;
            }
        }

        private void OnDisable()
        {
            if (_seasonSystem == null)
                return;

            _seasonSystem.OnSeasonChanged -= HandleSeasonChanged;
            _seasonSystem.OnSeasonTransitionProgress -= HandleTransition;
            _seasonSystem.OnSeasonVisualHook -= HandleVisualHook;
        }

        private void HandleSeasonChanged(SeasonId previous, SeasonId next)
        {
            OnSeasonAppearanceChanged?.Invoke(next, _seasonSystem != null ? _seasonSystem.TransitionProgress01 : 0f);
        }

        private void HandleTransition(float progress01)
        {
            if (_seasonSystem != null)
                OnSeasonAppearanceChanged?.Invoke(_seasonSystem.CurrentSeason, progress01);
        }

        private void HandleVisualHook()
        {
            if (_seasonSystem != null)
                OnSeasonAppearanceChanged?.Invoke(_seasonSystem.CurrentSeason, _seasonSystem.TransitionProgress01);
        }
    }
}
