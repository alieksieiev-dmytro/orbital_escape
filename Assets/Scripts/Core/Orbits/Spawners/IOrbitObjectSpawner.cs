using System.Collections.Generic;
using UnityEngine;

namespace OrbitalEscape.Orbits
{
    public interface IOrbitObjectSpawner
    {
        int Order { get; }
        void Spawn(GameObject orbitObject, Orbit orbit, IReadOnlyList<ArcObject> objects);
        void Clear();
    }
}
