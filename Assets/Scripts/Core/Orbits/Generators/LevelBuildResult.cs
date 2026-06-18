using System;
using System.Collections.Generic;
using OrbitalEscape.Orbits.Generators.Patterns;

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
        public IReadOnlyList<PatternDebugEntry> PatternDebugEntries { get; }
        public bool PatternDebugGizmosEnabled { get; }
        public IReadOnlyList<OrbitContentDelta> ContentDeltas { get; }

        public LevelBuildResult(
            IReadOnlyList<Orbit> orbits,
            IReadOnlyList<string> warnings,
            IReadOnlyList<PatternDebugEntry> patternDebugEntries = null,
            bool patternDebugGizmosEnabled = false,
            IReadOnlyList<OrbitContentDelta> contentDeltas = null)
        {
            Orbits = orbits;
            Warnings = warnings;
            PatternDebugEntries = patternDebugEntries ?? new List<PatternDebugEntry>();
            PatternDebugGizmosEnabled = patternDebugGizmosEnabled;
            ContentDeltas = contentDeltas ?? new List<OrbitContentDelta>();
        }
    }
}
