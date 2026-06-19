using System;
using System.Collections.Generic;

namespace OrbitalEscape.Orbits.Generators
{
    public readonly struct OrbitContentDelta
    {
        public int OrbitIndex { get; }
        public IReadOnlyList<ArcObject> AddedObjects { get; }

        public OrbitContentDelta(int orbitIndex, IReadOnlyList<ArcObject> addedObjects)
        {
            OrbitIndex = orbitIndex;
            AddedObjects = addedObjects ?? Array.Empty<ArcObject>();
        }
    }

    public readonly struct LevelBuildResult
    {
        public IReadOnlyList<Orbit> Orbits { get; }
        public IReadOnlyList<string> Warnings { get; }
        public IReadOnlyList<OrbitContentDelta> ContentDeltas { get; }

        public LevelBuildResult(
            IReadOnlyList<Orbit> orbits,
            IReadOnlyList<string> warnings,
            IReadOnlyList<OrbitContentDelta> contentDeltas = null)
        {
            Orbits = orbits;
            Warnings = warnings;
            ContentDeltas = contentDeltas ?? new List<OrbitContentDelta>();
        }
    }
}
