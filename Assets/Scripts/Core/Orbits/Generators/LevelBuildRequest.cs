using System.Collections.Generic;

namespace OrbitalEscape.Orbits.Generators
{
    public readonly struct LevelBuildRequest
    {
        public IReadOnlyList<Orbit> ExistingOrbits { get; }
        public int Count { get; }
        public int Seed { get; }
        public LevelBuildSettings Settings { get; }

        public LevelBuildRequest(
            IReadOnlyList<Orbit> existingOrbits,
            int count,
            int seed,
            LevelBuildSettings settings)
        {
            ExistingOrbits = existingOrbits;
            Count = count;
            Seed = seed;
            Settings = settings;
        }
    }
}
