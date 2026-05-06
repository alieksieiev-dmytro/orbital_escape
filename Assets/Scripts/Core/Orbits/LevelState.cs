using System.Collections.Generic;

namespace OrbitalEscape.Orbits
{
    public sealed class LevelState
    {
        private readonly List<Orbit> _orbits = new List<Orbit>();

        public IReadOnlyList<Orbit> Orbits => _orbits;
        public int Count => _orbits.Count;

        public Orbit GetOrbit(int index)
        {
            return _orbits[index];
        }

        public void Clear()
        {
            _orbits.Clear();
        }

        public void AddRange(IEnumerable<Orbit> orbits)
        {
            _orbits.AddRange(orbits);
        }
    }
}
