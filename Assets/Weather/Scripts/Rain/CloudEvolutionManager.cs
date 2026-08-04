using System.Collections.Generic;
using Game.Weather.Cloud;
using Game.Weather.Convergence;
using UnityEngine;
using CloudEntity = Game.Weather.Cloud.Cloud;

namespace Game.Weather.Rain
{
    /// <summary>
    /// Handles cloud evolution separate from lake spawn logic.
    /// Phase 1: visual crossfade from cluster cloud VFX into the rain band asset.
    /// </summary>
    [DefaultExecutionOrder(25)]
    public class CloudEvolutionManager : MonoBehaviour
    {
        [SerializeField] private CloudManager _cloudManager;
        [SerializeField] private ConvergenceManager _convergenceManager;

        [Header("Rain Visual")]
        [SerializeField] private GameObject _rainSystemPrefab;
        [SerializeField] private Transform _rainVisualParent;

        [Header("Cluster -> Rain (Test Hook)")]
        [Tooltip("When enabled, every cluster cloud will evolve into rain after spawn completes.")]
        [SerializeField] private bool _testEvolveClusterToRain;

        [Tooltip("Optional delay after the cluster finishes spawning before evolution starts.")]
        [SerializeField, Min(0f)] private float _evolutionDelayAfterSpawn = 2f;

        [SerializeField, Min(0.1f)] private float _transitionDurationSeconds = 6f;

        private readonly List<ClusterToRainTransition> _activeTransitions = new();
        private readonly HashSet<int> _scheduledCloudIds = new();
        private readonly Dictionary<int, float> _evolutionDelayTimers = new();

        private sealed class ClusterToRainTransition
        {
            public CloudEntity Cloud;
            public GameObject ClusterVisual;
            public GameObject RainVisual;
            public ParticleMaxParticlesGroup ClusterParticles;
            public ParticleMaxParticlesGroup RainParticles;
            public float Elapsed;
        }

        private void Awake()
        {
            if (_cloudManager == null)
                _cloudManager = FindFirstObjectByType<CloudManager>();

            if (_convergenceManager == null)
                _convergenceManager = FindFirstObjectByType<ConvergenceManager>();
        }

        private void Update()
        {
            if (_cloudManager == null)
                return;

            QueueEligibleClusterClouds();
            TrackEvolutionDelayTimers(Time.deltaTime);
            UpdateActiveTransitions(Time.deltaTime);
        }

        public void RequestEvolveToRain(int cloudId)
        {
            if (_cloudManager == null)
                return;

            _cloudManager.RequestEvolveToRain(cloudId);
        }

        private void QueueEligibleClusterClouds()
        {
            if (!_testEvolveClusterToRain)
                return;

            foreach (CloudEntity cloud in _cloudManager.ActiveClouds)
            {
                if (!IsEligibleClusterForEvolution(cloud))
                    continue;

                if (!HasEvolutionDelayElapsed(cloud.Id))
                    continue;

                _scheduledCloudIds.Add(cloud.Id);
                cloud.ShouldEvolveToRain = true;
            }
        }

        private bool IsEligibleClusterForEvolution(CloudEntity cloud)
        {
            if (cloud == null
                || cloud.VisualCategory != CloudVisualCategory.Cluster
                || cloud.IsRainBand
                || cloud.IsEvolvingToRain
                || _scheduledCloudIds.Contains(cloud.Id))
            {
                return false;
            }

            if (cloud.State != CloudState.Drifting && cloud.State != CloudState.Held)
                return false;

            return cloud.VisualObject != null && _rainSystemPrefab != null;
        }

        private bool HasEvolutionDelayElapsed(int cloudId)
        {
            if (_evolutionDelayAfterSpawn <= 0f)
                return true;

            return _evolutionDelayTimers.TryGetValue(cloudId, out float elapsed)
                   && elapsed >= _evolutionDelayAfterSpawn;
        }

        private void TrackEvolutionDelayTimers(float deltaTime)
        {
            if (_evolutionDelayAfterSpawn <= 0f)
                return;

            foreach (CloudEntity cloud in _cloudManager.ActiveClouds)
            {
                if (!IsEligibleClusterForEvolution(cloud))
                {
                    _evolutionDelayTimers.Remove(cloud.Id);
                    continue;
                }

                if (!_evolutionDelayTimers.ContainsKey(cloud.Id))
                    _evolutionDelayTimers[cloud.Id] = 0f;

                _evolutionDelayTimers[cloud.Id] += deltaTime;
            }
        }

        private void UpdateActiveTransitions(float deltaTime)
        {
            for (int i = _activeTransitions.Count - 1; i >= 0; i--)
            {
                ClusterToRainTransition transition = _activeTransitions[i];
                if (transition.Cloud == null || transition.Cloud.VisualObject == null)
                {
                    CleanupTransition(transition);
                    _activeTransitions.RemoveAt(i);
                    continue;
                }

                transition.Elapsed += deltaTime;
                float t = Mathf.Clamp01(transition.Elapsed / _transitionDurationSeconds);
                float smoothT = t * t * (3f - 2f * t);

                transition.ClusterParticles?.SetIntensity(1f - smoothT, rampMaxParticles: false);
                transition.RainParticles?.SetIntensity(smoothT, rampMaxParticles: true);

                if (transition.RainVisual != null && transition.ClusterVisual != null)
                {
                    transition.RainVisual.transform.position = transition.ClusterVisual.transform.position;
                }

                if (t < 1f)
                    continue;

                CompleteTransition(transition);
                _activeTransitions.RemoveAt(i);
            }

            TryStartPendingTransitions();
        }

        private void TryStartPendingTransitions()
        {
            foreach (CloudEntity cloud in _cloudManager.ActiveClouds)
            {
                if (!cloud.ShouldEvolveToRain
                    || cloud.IsEvolvingToRain
                    || cloud.IsRainBand
                    || cloud.VisualCategory != CloudVisualCategory.Cluster
                    || cloud.VisualObject == null
                    || IsTransitionActiveForCloud(cloud.Id))
                {
                    continue;
                }

                if (cloud.State != CloudState.Drifting && cloud.State != CloudState.Held)
                    continue;

                StartTransition(cloud);
            }
        }

        private bool IsTransitionActiveForCloud(int cloudId)
        {
            foreach (ClusterToRainTransition transition in _activeTransitions)
            {
                if (transition.Cloud != null && transition.Cloud.Id == cloudId)
                    return true;
            }

            return false;
        }

        private void StartTransition(CloudEntity cloud)
        {
            if (_rainSystemPrefab == null)
            {
                Debug.LogWarning("[CloudEvolution] RainSystem prefab is not assigned.");
                return;
            }

            _convergenceManager?.TryReleaseHeldCloud(cloud.Id, markAsReleased: false);

            cloud.IsEvolvingToRain = true;
            cloud.State = CloudState.EvolvingToRain;
            cloud.HasHoldTarget = false;

            GameObject clusterVisual = cloud.VisualObject;
            Transform parent = _rainVisualParent != null ? _rainVisualParent : clusterVisual.transform.parent;

            GameObject rainVisual = Instantiate(
                _rainSystemPrefab,
                clusterVisual.transform.position,
                clusterVisual.transform.rotation,
                parent);

            ParticleMaxParticlesGroup clusterParticles = ParticleMaxParticlesGroup.FromGameObject(clusterVisual);
            ParticleMaxParticlesGroup rainParticles = ParticleMaxParticlesGroup.FromGameObject(rainVisual);

            rainParticles?.SetIntensity(0f, rampMaxParticles: true);

            _activeTransitions.Add(new ClusterToRainTransition
            {
                Cloud = cloud,
                ClusterVisual = clusterVisual,
                RainVisual = rainVisual,
                ClusterParticles = clusterParticles,
                RainParticles = rainParticles,
                Elapsed = 0f
            });

            Debug.Log($"[CloudEvolution] Started cluster -> rain transition for cloud #{cloud.Id}");
        }

        private void CompleteTransition(ClusterToRainTransition transition)
        {
            CloudEntity cloud = transition.Cloud;
            if (cloud == null)
            {
                CleanupTransition(transition);
                return;
            }

            if (transition.ClusterVisual != null)
                Destroy(transition.ClusterVisual);

            cloud.VisualObject = transition.RainVisual;
            cloud.IsEvolvingToRain = false;
            cloud.IsRainBand = true;
            cloud.ShouldEvolveToRain = false;
            cloud.State = CloudState.RainBand;

            _scheduledCloudIds.Remove(cloud.Id);
            _evolutionDelayTimers.Remove(cloud.Id);

            Debug.Log($"[CloudEvolution] Cloud #{cloud.Id} is now a rain band.");
        }

        private static void CleanupTransition(ClusterToRainTransition transition)
        {
            if (transition.RainVisual != null && transition.Cloud != null && transition.Cloud.VisualObject != transition.RainVisual)
                Destroy(transition.RainVisual);
        }
    }
}
