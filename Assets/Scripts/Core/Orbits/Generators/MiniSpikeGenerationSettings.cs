namespace OrbitalEscape.Orbits.Generators
{
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
}
