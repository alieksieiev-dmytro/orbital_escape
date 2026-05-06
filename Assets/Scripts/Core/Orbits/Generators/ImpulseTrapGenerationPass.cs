using Zenject;

namespace OrbitalEscape.Orbits.Generators
{
    public sealed class ImpulseTrapGenerationPass : IOrbitContentPass
    {
        private readonly IArcGenerator _generator;

        public int Order => 20;

        [Inject]
        public ImpulseTrapGenerationPass([Inject(Id = ArcObjectType.ImpulseTrap)] IArcGenerator generator)
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
