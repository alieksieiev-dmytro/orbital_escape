using System.Collections.Generic;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    public interface ILevelContentGenerator
    {
        void Populate(
            IReadOnlyList<Orbit> generatedOrbits,
            LevelBuildContext context,
            LevelBuildRequest request);
    }
}
