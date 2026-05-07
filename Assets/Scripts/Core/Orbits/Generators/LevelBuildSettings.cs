using System;

namespace OrbitalEscape.Orbits.Generators
{
    public readonly struct LevelBuildSettings
    {
        public OrbitGenerationSettings Orbit { get; }
        public LevelContentSettings Content { get; }
        public RiftGenerationSettings Rift { get; }
        public SpikeGenerationSettings Spike { get; }
        public ImpulseTrapGenerationSettings ImpulseTrap { get; }

        public LevelBuildSettings(
            OrbitGenerationSettings orbit,
            LevelContentSettings content,
            RiftGenerationSettings rift,
            SpikeGenerationSettings spike,
            ImpulseTrapGenerationSettings impulseTrap)
        {
            Orbit = orbit;
            Content = content;
            Rift = rift;
            Spike = spike;
            ImpulseTrap = impulseTrap;
        }

        public bool ShouldGenerateContent(Orbit orbit)
        {
            return orbit.Index >= Content.FirstOrbitIndexWithContent;
        }
    }

    public readonly struct OrbitGenerationSettings
    {
        public float FirstOrbitRadius { get; }
        public float OrbitRadiusStep { get; }

        public OrbitGenerationSettings(float firstOrbitRadius, float orbitRadiusStep)
        {
            FirstOrbitRadius = firstOrbitRadius;
            OrbitRadiusStep = orbitRadiusStep;
        }
    }

    public readonly struct LevelContentSettings
    {
        public int FirstOrbitIndexWithContent { get; }

        public LevelContentSettings(int firstOrbitIndexWithContent)
        {
            FirstOrbitIndexWithContent = Math.Max(0, firstOrbitIndexWithContent);
        }
    }

    public readonly struct RiftGenerationSettings
    {
        public float MinDistanceBetweenRifts { get; }
        public float RiftLength { get; }
        public float RiftCount { get; }
        public float RiftPadding { get; }
        public int MaxPlacementAttempts { get; }

        public RiftGenerationSettings(
            float minDistanceBetweenRifts,
            float riftLength,
            float riftCount,
            float riftPadding,
            int maxPlacementAttempts)
        {
            MinDistanceBetweenRifts = minDistanceBetweenRifts;
            RiftLength = riftLength;
            RiftCount = riftCount;
            RiftPadding = riftPadding;
            MaxPlacementAttempts = Math.Max(1, maxPlacementAttempts);
        }
    }

    public readonly struct SpikeGenerationSettings
    {
        public float Density { get; }
        public float ConsecutiveSpikeChance { get; }
        public float MinSpikeHeight { get; }
        public float MaxSpikeHeight { get; }
        public float SpikeArcLength { get; }

        public SpikeGenerationSettings(
            float density,
            float consecutiveSpikeChance,
            float minSpikeHeight,
            float maxSpikeHeight,
            float spikeArcLength)
        {
            Density = Clamp01(density);
            ConsecutiveSpikeChance = Clamp01(consecutiveSpikeChance);
            MinSpikeHeight = minSpikeHeight;
            MaxSpikeHeight = maxSpikeHeight;
            SpikeArcLength = spikeArcLength;
        }

        private static float Clamp01(float value)
        {
            return Math.Min(Math.Max(value, 0.0f), 1.0f);
        }
    }

    public readonly struct ImpulseTrapGenerationSettings
    {
        public float Density { get; }
        public float TrapArcLength { get; }
        public float Height { get; }

        public ImpulseTrapGenerationSettings(float density, float trapArcLength, float height)
        {
            Density = Math.Min(Math.Max(density, 0.0f), 1.0f);
            TrapArcLength = trapArcLength;
            Height = height;
        }
    }
}
