using System.Collections.Generic;
using UnityEngine;

namespace OrbitalEscape.Orbits
{
    public interface ILevelObjectSpawner
    {
        IReadOnlyList<GameObject> SpawnedOrbitObjects { get; }
        GameObject SpawnOrbit(Orbit orbit);
        GameObject GetOrbitObject(int index);
        void Clear();
    }
}
