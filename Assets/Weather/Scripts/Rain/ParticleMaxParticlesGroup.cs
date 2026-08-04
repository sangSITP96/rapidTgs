using UnityEngine;

namespace Game.Weather.Rain
{
    public sealed class ParticleMaxParticlesGroup
    {
        private static readonly int ColorPropertyId = Shader.PropertyToID("Color_");
        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColorPropertyId = Shader.PropertyToID("_Color");

        private readonly SystemSnapshot[] _snapshots;

        private sealed class SystemSnapshot
        {
            public ParticleSystem System;
            public ParticleSystemRenderer Renderer;
            public MaterialPropertyBlock PropertyBlock;
            public int BaselineMaxParticles;
            public float BaselineEmissionMultiplier;
            public Color BaselineColor;
            public int ColorPropertyId;
            public bool HasColorProperty;
        }

        private ParticleMaxParticlesGroup(SystemSnapshot[] snapshots)
        {
            _snapshots = snapshots;
        }

        public static ParticleMaxParticlesGroup FromGameObject(GameObject root, bool includeInactive = true)
        {
            if (root == null)
                return null;

            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(includeInactive);
            if (systems == null || systems.Length == 0)
                return null;

            SystemSnapshot[] snapshots = new SystemSnapshot[systems.Length];

            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem system = systems[i];
                ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();

                snapshots[i] = new SystemSnapshot
                {
                    System = system,
                    Renderer = renderer,
                    PropertyBlock = new MaterialPropertyBlock(),
                    BaselineMaxParticles = system.main.maxParticles,
                    BaselineEmissionMultiplier = system.emission.rateOverTimeMultiplier
                };

                CaptureBaselineColor(snapshots[i]);
            }

            return new ParticleMaxParticlesGroup(snapshots);
        }

        public void SetIntensity(float intensity, bool rampMaxParticles)
        {
            intensity = Mathf.Clamp01(intensity);

            foreach (SystemSnapshot snapshot in _snapshots)
            {
                if (snapshot.System == null)
                    continue;

                ApplyEmission(snapshot, intensity);
                ApplyColor(snapshot, intensity);
                ApplyMaxParticles(snapshot, intensity, rampMaxParticles);
                ApplyPlayback(snapshot, intensity);
            }
        }

        private static void CaptureBaselineColor(SystemSnapshot snapshot)
        {
            if (snapshot.Renderer == null)
                return;

            Material sharedMaterial = snapshot.Renderer.sharedMaterial;
            if (sharedMaterial == null)
                return;

            if (TryReadColor(sharedMaterial, ColorPropertyId, out Color color))
            {
                snapshot.ColorPropertyId = ColorPropertyId;
            }
            else if (TryReadColor(sharedMaterial, BaseColorPropertyId, out color))
            {
                snapshot.ColorPropertyId = BaseColorPropertyId;
            }
            else if (TryReadColor(sharedMaterial, LegacyColorPropertyId, out color))
            {
                snapshot.ColorPropertyId = LegacyColorPropertyId;
            }
            else
            {
                return;
            }

            snapshot.HasColorProperty = true;
            snapshot.BaselineColor = color;
        }

        private static bool TryReadColor(Material material, int propertyId, out Color color)
        {
            if (material.HasProperty(propertyId))
            {
                color = material.GetColor(propertyId);
                return true;
            }

            color = default;
            return false;
        }

        private static void ApplyEmission(SystemSnapshot snapshot, float intensity)
        {
            ParticleSystem.EmissionModule emission = snapshot.System.emission;
            emission.rateOverTimeMultiplier = snapshot.BaselineEmissionMultiplier * intensity;
        }

        private static void ApplyColor(SystemSnapshot snapshot, float intensity)
        {
            if (!snapshot.HasColorProperty || snapshot.Renderer == null)
                return;

            Color color = snapshot.BaselineColor;
            color.a *= intensity;

            snapshot.Renderer.GetPropertyBlock(snapshot.PropertyBlock);
            snapshot.PropertyBlock.SetColor(snapshot.ColorPropertyId, color);
            snapshot.Renderer.SetPropertyBlock(snapshot.PropertyBlock);
        }

        private static void ApplyMaxParticles(SystemSnapshot snapshot, float intensity, bool rampMaxParticles)
        {
            ParticleSystem.MainModule main = snapshot.System.main;

            if (rampMaxParticles)
            {
                main.maxParticles = Mathf.Max(
                    0,
                    Mathf.RoundToInt(snapshot.BaselineMaxParticles * intensity));
                return;
            }

            main.maxParticles = snapshot.BaselineMaxParticles;
        }

        private static void ApplyPlayback(SystemSnapshot snapshot, float intensity)
        {
            if (intensity <= 0f)
            {
                if (snapshot.System.isPlaying)
                {
                    snapshot.System.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                }

                return;
            }

            if (!snapshot.System.isPlaying)
                snapshot.System.Play(true);
        }
    }
}
