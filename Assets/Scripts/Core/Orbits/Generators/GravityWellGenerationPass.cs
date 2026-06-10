using Zenject;

namespace OrbitalEscape.Orbits.Generators
{
    public sealed class GravityWellGenerationPass : IOrbitContentPass
    {
        private readonly IArcGenerator _generator;

        public int Order => 30;

        [Inject]
        public GravityWellGenerationPass([Inject(Id = ArcObjectType.GravityWell)] IArcGenerator generator)
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
