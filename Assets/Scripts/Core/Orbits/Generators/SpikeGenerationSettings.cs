using System;

namespace OrbitalEscape.Orbits.Generators
{
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
}
