using System.Collections.Generic;
using System.Linq;

namespace OrbitalEscape.Orbits.Generators
{
    public sealed class LevelBuildContext
    {
        private readonly List<Orbit> _orbits;
        private readonly List<string> _warnings;

        public IReadOnlyList<Orbit> Orbits => _orbits;
        public IReadOnlyList<string> Warnings => _warnings;
        public int StartIndex { get; }
        public int LastPlannedOrbitIndex { get; }
        public LevelBuildSettings Settings { get; }
        public LevelRandom Random { get; }

        public LevelBuildContext(
            IEnumerable<Orbit> existingOrbits,
            int buildCount,
            LevelBuildSettings settings,
            LevelRandom random)
        {
            _orbits = existingOrbits.ToList();
            _warnings = new List<string>();
            StartIndex = _orbits.Count;
            LastPlannedOrbitIndex = StartIndex + buildCount - 1;
            Settings = settings;
            Random = random;
        }

        public bool TryGetPreviousOrbit(out Orbit orbit)
        {
            if (_orbits.Count == 0)
            {
                orbit = null;
                return false;
            }

            orbit = _orbits[^1];
            return true;
        }

        internal void RegisterBuiltOrbit(Orbit orbit)
        {
            _orbits.Add(orbit);
        }

        public void AddWarning(string warning)
        {
            _warnings.Add(warning);
        }
    }
}
