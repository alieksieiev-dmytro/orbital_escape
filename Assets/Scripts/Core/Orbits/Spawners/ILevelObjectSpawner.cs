using System.Collections.Generic;
using UnityEngine;

namespace OrbitalEscape.Orbits
{
    public interface ILevelObjectSpawner
    {
        IReadOnlyList<GameObject> SpawnedOrbitObjects { get; }
        GameObject SpawnOrbit(Orbit orbit);
        void AppendOrbitContent(Orbit orbit, IReadOnlyList<ArcObject> addedObjects);
        GameObject GetOrbitObject(int index);
        void Clear();
    }
}
