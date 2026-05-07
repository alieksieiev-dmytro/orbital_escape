using System.Collections.Generic;

namespace OrbitalEscape.Orbits.Generators
{
    public readonly struct LevelBuildResult
    {
        public IReadOnlyList<Orbit> Orbits { get; }
        public IReadOnlyList<string> Warnings { get; }

        public LevelBuildResult(IReadOnlyList<Orbit> orbits, IReadOnlyList<string> warnings)
        {
            Orbits = orbits;
            Warnings = warnings;
        }
    }
}
