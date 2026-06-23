using System;

namespace OrbitalEscape.Orbits.Generators
{
    using Patterns;

    public readonly struct LevelBuildSettings
    {
        public OrbitGenerationSettings Orbit { get; }
        public LevelContentSettings Content { get; }
        public RiftGenerationSettings Rift { get; }
        public SpikeGenerationSettings Spike { get; }
        public MiniSpikeGenerationSettings MiniSpike { get; }
        public ImpulseTrapGenerationSettings ImpulseTrap { get; }
        public GravityWellGenerationSettings GravityWell { get; }
        public PatternGenerationSettings Patterns { get; }
        public bool RandomizeBandPhase { get; }

        public LevelBuildSettings(
            OrbitGenerationSettings orbit,
            LevelContentSettings content,
            RiftGenerationSettings rift,
            SpikeGenerationSettings spike,
            MiniSpikeGenerationSettings miniSpike,
            ImpulseTrapGenerationSettings impulseTrap,
            GravityWellGenerationSettings gravityWell,
            PatternGenerationSettings patterns,
            bool randomizeBandPhase)
        {
            Orbit = orbit;
            Content = content;
            Rift = rift;
            Spike = spike;
            MiniSpike = miniSpike;
            ImpulseTrap = impulseTrap;
            GravityWell = gravityWell;
            Patterns = patterns;
            RandomizeBandPhase = randomizeBandPhase;
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
        public float BaseRiftCountRadius { get; }
        public float AdditionalRiftRadiusStep { get; }
        public float RiftWeightIncreasePerNonRiftPattern { get; }
        public float MaxRiftPressureMultiplier { get; }
        public float NeighborRiftAvoidanceAngle { get; }
        public float AlignedNeighborRiftWeightMultiplier { get; }
        public float RiftPadding { get; }
        public int MaxPlacementAttempts { get; }

        public RiftGenerationSettings(
            float minDistanceBetweenRifts,
            float riftLength,
            float riftCount,
            float baseRiftCountRadius,
            float additionalRiftRadiusStep,
            float riftWeightIncreasePerNonRiftPattern,
            float maxRiftPressureMultiplier,
            float neighborRiftAvoidanceAngle,
            float alignedNeighborRiftWeightMultiplier,
            float riftPadding,
            int maxPlacementAttempts)
        {
            MinDistanceBetweenRifts = minDistanceBetweenRifts;
            RiftLength = riftLength;
            RiftCount = riftCount;
            BaseRiftCountRadius = Math.Max(0.01f, baseRiftCountRadius);
            AdditionalRiftRadiusStep = Math.Max(0.01f, additionalRiftRadiusStep);
            RiftWeightIncreasePerNonRiftPattern = Math.Max(0.0f, riftWeightIncreasePerNonRiftPattern);
            MaxRiftPressureMultiplier = Math.Max(1.0f, maxRiftPressureMultiplier);
            NeighborRiftAvoidanceAngle = Math.Min(180.0f, Math.Max(0.01f, neighborRiftAvoidanceAngle));
            AlignedNeighborRiftWeightMultiplier = Math.Min(
                1.0f,
                Math.Max(0.01f, alignedNeighborRiftWeightMultiplier));
            RiftPadding = riftPadding;
            MaxPlacementAttempts = Math.Max(1, maxPlacementAttempts);
        }

        public int GetTargetCount(Orbit orbit)
        {
            int baseCount = Math.Max(0, (int)Math.Ceiling(RiftCount));
            if (orbit == null)
            {
                return baseCount;
            }

            float radiusDelta = Math.Max(0.0f, orbit.Radius - BaseRiftCountRadius);
            int additionalCount = (int)Math.Floor(radiusDelta / AdditionalRiftRadiusStep);
            return baseCount + additionalCount;
        }

        public float GetPressureMultiplier(int consecutiveNonRiftPatterns)
        {
            float multiplier = 1.0f +
                               Math.Max(0, consecutiveNonRiftPatterns) *
                               RiftWeightIncreasePerNonRiftPattern;
            return Math.Min(multiplier, MaxRiftPressureMultiplier);
        }

        public float GetNeighborWeightMultiplier(float angularDistance)
        {
            float normalizedDistance = Math.Min(
                1.0f,
                Math.Max(0.0f, angularDistance) / NeighborRiftAvoidanceAngle);
            return AlignedNeighborRiftWeightMultiplier +
                   (1.0f - AlignedNeighborRiftWeightMultiplier) * normalizedDistance;
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

    public readonly struct MiniSpikeGenerationSettings
    {
        public float Height { get; }
        public float SpikeArcLength { get; }

        public MiniSpikeGenerationSettings(float height, float spikeArcLength)
        {
            Height = height;
            SpikeArcLength = spikeArcLength;
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

    public readonly struct GravityWellGenerationSettings
    {
        public float Density { get; }
        public float FieldArcLength { get; }
        public float PartialJumpStep { get; }
        public float PullSpeed { get; }
        public float ReturnDuration { get; }
        public float VisualAlpha { get; }

        public GravityWellGenerationSettings(
            float density,
            float fieldArcLength,
            float partialJumpStep,
            float pullSpeed,
            float returnDuration,
            float visualAlpha)
        {
            Density = Math.Min(Math.Max(density, 0.0f), 1.0f);
            FieldArcLength = fieldArcLength;
            PartialJumpStep = Math.Min(Math.Max(partialJumpStep, 0.0f), 1.0f);
            PullSpeed = Math.Max(0.0f, pullSpeed);
            ReturnDuration = Math.Max(0.01f, returnDuration);
            VisualAlpha = Math.Min(Math.Max(visualAlpha, 0.0f), 1.0f);
        }
    }
}
