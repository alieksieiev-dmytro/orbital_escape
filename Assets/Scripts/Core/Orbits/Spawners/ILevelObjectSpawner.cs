using UnityEngine;

namespace OrbitalEscape.Orbits
{
    public interface ILevelObjectSpawner
    {
        GameObject SpawnOrbit(Orbit orbit);
        GameObject GetOrbitObject(int index);
        void Clear();
    }
}
