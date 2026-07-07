using System.Collections.Generic;

namespace OrbitalEscape.Orbits
{
    public sealed class RiftClosureState : IRiftClosureState
    {
        private readonly HashSet<int> _closedOrbitIndices = new HashSet<int>();

        public bool IsClosed(int orbitIndex)
        {
            return _closedOrbitIndices.Contains(orbitIndex);
        }

        public bool Close(int orbitIndex)
        {
            return orbitIndex >= 0 && _closedOrbitIndices.Add(orbitIndex);
        }

        public void Clear()
        {
            _closedOrbitIndices.Clear();
        }
    }
}
