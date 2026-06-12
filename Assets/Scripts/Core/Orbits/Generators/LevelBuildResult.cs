using System.Collections.Generic;
using OrbitalEscape.Orbits.Generators.Patterns;

namespace OrbitalEscape.Orbits.Generators
{
    public readonly struct LevelBuildResult
    {
        public IReadOnlyList<Orbit> Orbits { get; }
        public IReadOnlyList<string> Warnings { get; }
        public IReadOnlyList<PatternDebugEntry> PatternDebugEntries { get; }
        public bool PatternDebugGizmosEnabled { get; }

        public LevelBuildResult(
            IReadOnlyList<Orbit> orbits,
            IReadOnlyList<string> warnings,
            IReadOnlyList<PatternDebugEntry> patternDebugEntries = null,
            bool patternDebugGizmosEnabled = false)
        {
            Orbits = orbits;
            Warnings = warnings;
            PatternDebugEntries = patternDebugEntries ?? new List<PatternDebugEntry>();
            PatternDebugGizmosEnabled = patternDebugGizmosEnabled;
        }
    }
}
