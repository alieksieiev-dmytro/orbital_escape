using System.Collections.Generic;
using System.Linq;

namespace OrbitalEscape.Orbits.Generators
{
    public sealed class LevelBuildContext
    {
        private readonly List<Orbit> _orbits;

        public IReadOnlyList<Orbit> Orbits => _orbits;
        public int StartIndex { get; }
        public LevelGenerationConfig GenerationConfig { get; }

        public LevelBuildContext(IEnumerable<Orbit> existingOrbits, LevelGenerationConfig generationConfig)
        {
            _orbits = existingOrbits.ToList();
            StartIndex = _orbits.Count;
            GenerationConfig = generationConfig;
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
    }
}
