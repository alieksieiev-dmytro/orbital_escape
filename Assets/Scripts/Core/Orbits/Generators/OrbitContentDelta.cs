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
}
