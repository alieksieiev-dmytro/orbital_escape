using Zenject;

namespace OrbitalEscape.Orbits.Generators
{
    public sealed class SpikeGenerationPass : IOrbitContentPass
    {
        private readonly IArcGenerator _generator;

        public int Order => 10;

        [Inject]
        public SpikeGenerationPass([Inject(Id = ArcObjectType.Spike)] IArcGenerator generator)
        {
            _generator = generator;
        }

        public void Populate(Orbit orbit, LevelBuildContext context)
        {
            orbit.OrbitObjects.AddRange(_generator.GenerateArcs(orbit, ArcType.Outer, context));
            orbit.OrbitObjects.AddRange(_generator.GenerateArcs(orbit, ArcType.Inner, context));
        }
    }
}
