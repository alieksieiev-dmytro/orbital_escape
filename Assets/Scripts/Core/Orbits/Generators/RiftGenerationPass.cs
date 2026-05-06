using Zenject;

namespace OrbitalEscape.Orbits.Generators
{
    public sealed class RiftGenerationPass : IOrbitContentPass
    {
        private readonly IArcGenerator _generator;

        public int Order => 0;

        [Inject]
        public RiftGenerationPass([Inject(Id = ArcObjectType.Rift)] IArcGenerator generator)
        {
            _generator = generator;
        }

        public void Populate(Orbit orbit, LevelBuildContext context)
        {
            orbit.OrbitObjects.AddRange(_generator.GenerateArcs(orbit, ArcType.TwoSide, context));
        }
    }
}
