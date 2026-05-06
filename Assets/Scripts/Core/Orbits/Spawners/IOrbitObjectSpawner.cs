using UnityEngine;

namespace OrbitalEscape.Orbits
{
    public interface IOrbitObjectSpawner
    {
        int Order { get; }
        void Spawn(GameObject orbitObject, Orbit orbit);
        void Clear();
    }
}
