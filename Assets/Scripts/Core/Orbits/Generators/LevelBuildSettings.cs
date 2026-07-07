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
        public LaserGateGenerationSettings LaserGate { get; }
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
            LaserGateGenerationSettings laserGate,
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
            LaserGate = laserGate;
            Patterns = patterns;
            RandomizeBandPhase = randomizeBandPhase;
        }

        public bool ShouldGenerateContent(Orbit orbit)
        {
            return orbit.Index >= Content.FirstOrbitIndexWithContent;
        }
    }
}
