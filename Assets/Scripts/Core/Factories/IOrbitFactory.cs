using OrbitalEscape.Orbits;
using UnityEngine;

namespace OrbitalEscape.Factories
{
    public interface IOrbitFactory
    {
        GameObject GetOrCreate(Orbit orbit);
        GameObject Get(int index);
        void DeactivateAll();
    }
}
